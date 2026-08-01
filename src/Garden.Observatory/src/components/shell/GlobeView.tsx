import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import * as THREE from 'three'
import { fetchMap } from '@/lib/api'
import { getTerrainColor } from '@/lib/terrainColors'

// Deterministic PRNG so the cloud pattern is stable across re-renders
// instead of reshuffling every time mapData refetches.
function mulberry32(seed: number) {
  let a = seed
  return () => {
    a |= 0
    a = (a + 0x6d2b79f5) | 0
    let t = Math.imul(a ^ (a >>> 15), 1 | a)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

interface GlobeViewProps {
  worldWidth: number
  worldHeight: number
  onSelectTile: (x: number, y: number) => void
}

/**
 * The "see the whole planet" tier, rendered as an actual rotatable sphere
 * on a starfield instead of a flat letterboxed square - the payoff for
 * zooming all the way out should feel like leaving the map and looking at
 * a world, not just a smaller crop of the same 2D grid. Fetches the whole
 * world once (panning doesn't apply to a globe - you rotate it instead) and
 * paints it onto an offscreen canvas as an equirectangular-ish texture,
 * reusing the same terrain palette as the flat map so the two views agree.
 */
export function GlobeView({ worldWidth, worldHeight, onSelectTile }: GlobeViewProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const onSelectTileRef = useRef(onSelectTile)
  const [hoverTile, setHoverTile] = useState<{ x: number; y: number; screenX: number; screenY: number } | null>(null)

  useEffect(() => {
    onSelectTileRef.current = onSelectTile
  }, [onSelectTile])

  const { data: mapData } = useQuery({
    queryKey: ['world-map-globe', worldWidth, worldHeight],
    queryFn: () => fetchMap(0, 0, worldWidth, worldHeight),
    refetchInterval: 20000,
  })

  useEffect(() => {
    const container = containerRef.current
    if (!container || !mapData?.tiles) return

    const tileMap = new Map<string, (typeof mapData.tiles)[number]>()
    for (const t of mapData.tiles) tileMap.set(`${t.x},${t.y}`, t)

    // Terrain texture - painted at a low base resolution (one block per
    // tile, cheap to compute) then smoothly upscaled onto a much larger
    // canvas so the sphere reads as dense, continuous terrain rather than
    // visibly blocky squares - the flat map's per-tile grid is the point
    // there, but a globe is meant to look like a planet, not a spreadsheet.
    const texScale = 2
    const texCanvas = document.createElement('canvas')
    texCanvas.width = worldWidth * texScale
    texCanvas.height = worldHeight * texScale
    const tctx = texCanvas.getContext('2d')!
    function colorAt(x: number, y: number) {
      const tile = tileMap.get(`${((x % worldWidth) + worldWidth) % worldWidth},${y}`)
      return tile ? getTerrainColor(tile.terrain, tile.isRiver, tile.isLake).color : '#0b2a44'
    }
    for (let y = 0; y < worldHeight; y++) {
      for (let x = 0; x < worldWidth; x++) {
        tctx.fillStyle = colorAt(x, y)
        tctx.fillRect(x * texScale, y * texScale, texScale, texScale)
      }
    }
    // The world map is a bounded grid, not generated as seamlessly
    // wrapping terrain - column 0 and the last column are geologically
    // unrelated, which reads as a hard, jarring seam once wrapped around a
    // sphere. Cross-fading a few columns on each side of the wrap softens
    // it into a blend instead of a visible line.
    const seamWidth = Math.max(2, Math.round(worldWidth * 0.02))
    for (let y = 0; y < worldHeight; y++) {
      for (let i = -seamWidth; i < seamWidth; i++) {
        const t = (i + seamWidth) / (seamWidth * 2)
        const a = new THREE.Color(colorAt(i, y))
        const b = new THREE.Color(colorAt(i + worldWidth, y))
        const blended = a.clone().lerp(b, t)
        tctx.fillStyle = `#${blended.getHexString()}`
        const x = ((i % worldWidth) + worldWidth) % worldWidth
        tctx.fillRect(x * texScale, y * texScale, texScale, texScale)
      }
    }
    // Smooth upscale pass: the browser's own bilinear image scaling blends
    // the hard tile edges into continuous gradients, then a light blur
    // softens the remaining high-frequency detail - this is what actually
    // makes the globe read as dense terrain instead of visible squares.
    const upscale = 4
    const smoothCanvas = document.createElement('canvas')
    smoothCanvas.width = texCanvas.width * upscale
    smoothCanvas.height = texCanvas.height * upscale
    const sctx = smoothCanvas.getContext('2d')!
    sctx.imageSmoothingEnabled = true
    sctx.imageSmoothingQuality = 'high'
    sctx.filter = 'blur(3px)'
    sctx.drawImage(texCanvas, 0, 0, smoothCanvas.width, smoothCanvas.height)

    const texture = new THREE.CanvasTexture(smoothCanvas)
    texture.colorSpace = THREE.SRGBColorSpace
    texture.minFilter = THREE.LinearMipmapLinearFilter
    texture.magFilter = THREE.LinearFilter
    texture.anisotropy = 4
    texture.generateMipmaps = true
    // Explicit repeat wrap (rather than the default clamp-to-edge) avoids a
    // visible seam artifact from UV sampling exactly at the u=0/u=1 wrap on
    // a sphere - a well-known gotcha independent of the data seam above.
    texture.wrapS = THREE.RepeatWrapping
    texture.wrapT = THREE.ClampToEdgeWrapping

    // Cloud layer - a second, slightly larger sphere with a procedural
    // scattered-blob texture, rotating at its own slower independent speed
    // so weather visibly drifts relative to the terrain beneath it instead
    // of being painted onto the same surface.
    const cloudCanvas = document.createElement('canvas')
    cloudCanvas.width = 512
    cloudCanvas.height = 256
    const cctx = cloudCanvas.getContext('2d')!
    const cloudSeed = mulberry32(1337)
    for (let i = 0; i < 90; i++) {
      const cx = cloudSeed() * cloudCanvas.width
      const cy = cloudSeed() * cloudCanvas.height
      const r = 14 + cloudSeed() * 34
      const grad = cctx.createRadialGradient(cx, cy, 0, cx, cy, r)
      grad.addColorStop(0, 'rgba(255,255,255,0.85)')
      grad.addColorStop(1, 'rgba(255,255,255,0)')
      cctx.fillStyle = grad
      cctx.beginPath()
      cctx.arc(cx, cy, r, 0, Math.PI * 2)
      cctx.fill()
    }
    const cloudTexture = new THREE.CanvasTexture(cloudCanvas)
    cloudTexture.wrapS = THREE.RepeatWrapping
    cloudTexture.wrapT = THREE.ClampToEdgeWrapping

    const scene = new THREE.Scene()
    const camera = new THREE.PerspectiveCamera(45, 1, 0.1, 100)
    camera.position.z = 1.65

    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true })
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
    container.appendChild(renderer.domElement)

    // Starfield - a large point cloud on a sphere well outside the globe,
    // so it reads as a fixed backdrop rather than an object you can orbit
    // past.
    const starCount = 2200
    const starPositions = new Float32Array(starCount * 3)
    for (let i = 0; i < starCount; i++) {
      const r = 40 + Math.random() * 20
      const theta = Math.random() * Math.PI * 2
      const phi = Math.acos(2 * Math.random() - 1)
      starPositions[i * 3] = r * Math.sin(phi) * Math.cos(theta)
      starPositions[i * 3 + 1] = r * Math.sin(phi) * Math.sin(theta)
      starPositions[i * 3 + 2] = r * Math.cos(phi)
    }
    const starGeometry = new THREE.BufferGeometry()
    starGeometry.setAttribute('position', new THREE.BufferAttribute(starPositions, 3))
    const starMaterial = new THREE.PointsMaterial({ color: 0xffffff, size: 0.12, sizeAttenuation: true })
    const stars = new THREE.Points(starGeometry, starMaterial)
    scene.add(stars)

    const globeGroup = new THREE.Group()
    scene.add(globeGroup)

    const sphereGeometry = new THREE.SphereGeometry(1, 96, 96)
    const sphereMaterial = new THREE.MeshStandardMaterial({ map: texture, roughness: 0.85, metalness: 0.05 })
    const sphere = new THREE.Mesh(sphereGeometry, sphereMaterial)
    globeGroup.add(sphere)

    const cloudGeometry = new THREE.SphereGeometry(1.012, 64, 64)
    const cloudMaterial = new THREE.MeshStandardMaterial({
      map: cloudTexture,
      transparent: true,
      opacity: 0.75,
      depthWrite: false,
    })
    const clouds = new THREE.Mesh(cloudGeometry, cloudMaterial)
    globeGroup.add(clouds)

    // Fresnel rim glow - the classic cheap "atmospheric scattering" trick:
    // a slightly larger backside-rendered sphere that only lights up at
    // grazing angles, additively blended so it reads as a soft glowing
    // halo instead of a solid shell. This is the "planet in space" cosmos
    // read a flat translucent sphere doesn't sell on its own.
    const atmosphereGeometry = new THREE.SphereGeometry(1.06, 64, 64)
    const atmosphereMaterial = new THREE.ShaderMaterial({
      uniforms: { glowColor: { value: new THREE.Color(0x6db7ff) } },
      vertexShader: `
        varying vec3 vNormal;
        void main() {
          vNormal = normalize(normalMatrix * normal);
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: `
        uniform vec3 glowColor;
        varying vec3 vNormal;
        void main() {
          float intensity = pow(0.7 - dot(vNormal, vec3(0.0, 0.0, 1.0)), 3.0);
          gl_FragColor = vec4(glowColor, 1.0) * intensity;
        }
      `,
      side: THREE.BackSide,
      blending: THREE.AdditiveBlending,
      transparent: true,
      depthWrite: false,
    })
    globeGroup.add(new THREE.Mesh(atmosphereGeometry, atmosphereMaterial))

    scene.add(new THREE.AmbientLight(0xffffff, 0.55))
    const sun = new THREE.DirectionalLight(0xffffff, 1.1)
    sun.position.set(4, 2, 3)
    scene.add(sun)

    globeGroup.rotation.y = -Math.PI / 2

    let rafId: number
    let disposed = false
    let dragging = false
    let lastX = 0
    let lastY = 0
    // Separate from lastX/lastY (which move every frame during a drag) -
    // this anchors the click-vs-drag threshold to the total distance
    // travelled since pointerdown, not just the final micro-movement
    // before release, which was always ~0 and made every drag also fire a
    // tile-select click at wherever the cursor ended up.
    let downX = 0
    let downY = 0
    let velocityY = 0.0009
    let velocityX = 0
    const raycaster = new THREE.Raycaster()
    const pointerNdc = new THREE.Vector2()

    function resize() {
      const rect = container!.getBoundingClientRect()
      renderer.setSize(rect.width, rect.height)
      camera.aspect = rect.width / Math.max(1, rect.height)
      camera.updateProjectionMatrix()
    }
    resize()
    const resizeObserver = new ResizeObserver(resize)
    resizeObserver.observe(container)

    function uvToTile(u: number, v: number) {
      const x = Math.floor(u * worldWidth) % worldWidth
      const y = Math.floor((1 - v) * worldHeight)
      return { x: (x + worldWidth) % worldWidth, y: Math.max(0, Math.min(worldHeight - 1, y)) }
    }

    function pick(clientX: number, clientY: number) {
      const rect = container!.getBoundingClientRect()
      pointerNdc.x = ((clientX - rect.left) / rect.width) * 2 - 1
      pointerNdc.y = -((clientY - rect.top) / rect.height) * 2 + 1
      raycaster.setFromCamera(pointerNdc, camera)
      const hit = raycaster.intersectObject(sphere, false)[0]
      if (!hit?.uv) return null
      const { x, y } = uvToTile(hit.uv.x, hit.uv.y)
      return { x, y, screenX: clientX - rect.left, screenY: clientY - rect.top }
    }

    function onPointerDown(e: PointerEvent) {
      dragging = true
      lastX = e.clientX
      lastY = e.clientY
      downX = e.clientX
      downY = e.clientY
      renderer.domElement.setPointerCapture(e.pointerId)
    }
    function onPointerMove(e: PointerEvent) {
      if (dragging) {
        const dx = e.clientX - lastX
        const dy = e.clientY - lastY
        lastX = e.clientX
        lastY = e.clientY
        velocityY = dx * 0.0035
        velocityX = dy * 0.0035
        globeGroup.rotation.y += velocityY
        globeGroup.rotation.x = Math.max(-1.1, Math.min(1.1, globeGroup.rotation.x + velocityX))
        setHoverTile(null)
        return
      }
      const hit = pick(e.clientX, e.clientY)
      setHoverTile(hit)
    }
    function onPointerUp(e: PointerEvent) {
      if (!dragging) return
      dragging = false
      const dx = e.clientX - downX
      const dy = e.clientY - downY
      if (Math.abs(dx) < 3 && Math.abs(dy) < 3) {
        const hit = pick(e.clientX, e.clientY)
        if (hit) onSelectTileRef.current(hit.x, hit.y)
      }
    }
    function onPointerLeave() {
      if (!dragging) setHoverTile(null)
    }
    function onWheel(e: WheelEvent) {
      e.preventDefault()
      camera.position.z = Math.max(1.25, Math.min(3.5, camera.position.z + e.deltaY * 0.0025))
    }

    renderer.domElement.addEventListener('pointerdown', onPointerDown)
    renderer.domElement.addEventListener('pointermove', onPointerMove)
    renderer.domElement.addEventListener('pointerup', onPointerUp)
    renderer.domElement.addEventListener('pointerleave', onPointerLeave)
    renderer.domElement.addEventListener('wheel', onWheel, { passive: false })
    renderer.domElement.style.cursor = 'grab'

    function animate() {
      if (disposed) return
      if (!dragging) {
        // Gentle idle drift - "the world keeps turning" even if nobody
        // touches it, plus residual momentum from the last drag decaying
        // out instead of stopping dead.
        globeGroup.rotation.y += velocityY
        velocityX *= 0.94
        velocityY += (0.0009 - velocityY) * 0.02
      }
      // Clouds drift independently of the terrain beneath them regardless
      // of drag state, same as real weather not being locked to the ground.
      clouds.rotation.y += 0.00025
      renderer.render(scene, camera)
      rafId = requestAnimationFrame(animate)
    }
    animate()

    return () => {
      disposed = true
      cancelAnimationFrame(rafId)
      resizeObserver.disconnect()
      renderer.domElement.removeEventListener('pointerdown', onPointerDown)
      renderer.domElement.removeEventListener('pointermove', onPointerMove)
      renderer.domElement.removeEventListener('pointerup', onPointerUp)
      renderer.domElement.removeEventListener('pointerleave', onPointerLeave)
      renderer.domElement.removeEventListener('wheel', onWheel)
      sphereGeometry.dispose()
      sphereMaterial.dispose()
      cloudGeometry.dispose()
      cloudMaterial.dispose()
      cloudTexture.dispose()
      atmosphereGeometry.dispose()
      atmosphereMaterial.dispose()
      starGeometry.dispose()
      starMaterial.dispose()
      texture.dispose()
      renderer.dispose()
      if (renderer.domElement.parentElement === container) container.removeChild(renderer.domElement)
    }
  }, [mapData, worldWidth, worldHeight])

  return (
    <div ref={containerRef} className="relative h-full w-full overflow-hidden rounded-md border bg-[#050912]">
      {hoverTile && (
        <div
          className="pointer-events-none absolute z-10 rounded-md border bg-card px-2 py-1 text-xs font-medium text-card-foreground shadow-md"
          style={{ left: hoverTile.screenX + 14, top: hoverTile.screenY + 14 }}
        >
          ({hoverTile.x}, {hoverTile.y})
        </div>
      )}
    </div>
  )
}
