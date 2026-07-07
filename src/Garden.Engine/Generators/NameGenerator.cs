namespace Garden.Engine.Generators;

public static class NameGenerator
{
    private static readonly string[] FirstNames =
    [
        "Aria", "Borin", "Cedra", "Doran", "Elara", "Fenn", "Greta", "Hadrian",
        "Ivy", "Jorin", "Kara", "Liam", "Mira", "Norn", "Orin", "Pella",
        "Quinn", "Rina", "Soren", "Tessa", "Ulric", "Vena", "Wren", "Xara",
        "Yorik", "Zia", "Aldric", "Bria", "Corin", "Darya", "Emmett", "Freya",
        "Garen", "Hanna", "Isolde", "Jace", "Kael", "Lyra", "Milo", "Nadia",
        "Owen", "Piper", "Rune", "Sage", "Theo", "Una", "Vance", "Willa",
        "Xander", "Yara", "Zane", "Astrid", "Bran", "Clara", "Dustin", "Elowen",
        "Finn", "Gemma", "Hugo", "Iris", "Jasper", "Kira", "Leif", "Nova"
    ];

    private static readonly string[] LastNames =
    [
        "Ashford", "Blackwood", "Crestwell", "Dunmore", "Eversong", "Fernwood",
        "Greyvale", "Holloway", "Ironwood", "Jadebrook", "Kingsley", "Larkin",
        "Mosswood", "Northwind", "Oakheart", "Pinecrest", "Quillford", "Ravenwood",
        "Stormwatch", "Thornfield", "Underwood", "Valewind", "Whitmore", "Yarrow"
    ];

    public static string GenerateFirstName()
    {
        return FirstNames[System.Random.Shared.Next(FirstNames.Length)];
    }

    public static string GenerateLastName()
    {
        return LastNames[System.Random.Shared.Next(LastNames.Length)];
    }
}
