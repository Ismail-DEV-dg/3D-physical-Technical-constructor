namespace BrandingGenerator;

internal static class MarkCatalog
{
    public const string Background = "#0A0E17";
    public const string Cyan = "#00E5FF";
    public const string Violet = "#8B5CFF";
    public const string Green = "#39FF88";

    public static IReadOnlyList<Mark> Candidates { get; } =
    [
        new("chip-trace", 128, 26,
            [new("M94 36 H55 L35 56 V74 L53 92 H82", Cyan, 12),
             new("M102 60 H83 L67 76 H55", Violet, 9),
             new("M51 20 V26 M72 20 V26 M20 53 H25 M20 74 H25 M55 102 V108 M75 102 V108", Violet, 5)],
            [new(94, 36, 7, Cyan), new(102, 60, 6, Violet), new(82, 92, 8, Green)]),
        new("board-gate", 128, 26,
            [new("M32 43 H60 Q86 43 92 64 Q86 85 60 85 H32", Cyan, 10),
             new("M22 57 H47 M22 72 H47 M96 64 H109", Violet, 8),
             new("M62 20 V30 M62 98 V108", Violet, 6)],
            [new(109, 64, 8, Green)]),
        new("pulse-loop", 128, 26,
            [new("M99 49 V39 L87 27 H43 L29 41 V55 M29 79 V89 L41 101 H85 L99 87 V74", Violet, 9),
             new("M20 68 H45 L56 48 L69 82 L81 63 H107", Cyan, 9)],
            [new(107, 63, 7, Green)])
    ];

    public static Mark Small(string name, int size) => (name, size) switch
    {
        ("chip-trace", 16) => new(name, size, 3,
            [new("M11 4 H7 L4 7 V9 L6 11 H10", Cyan, 2), new("M13 7 H10 L8 9", Violet, 1.8)],
            [new(11, 11, 1.65, Green)]),
        ("chip-trace", 24) => new(name, size, 5,
            [new("M18 7 H10 L6 11 V14 L10 18 H15", Cyan, 2.7), new("M19 11 H15 L12 14", Violet, 2.3)],
            [new(16, 18, 2, Green)]),
        ("chip-trace", 32) => new(name, size, 6,
            [new("M23 9 H14 L9 14 V18 L13 23 H20", Cyan, 3.4),
             new("M26 15 H21 L17 19 H14 M13 5 V6 M18 5 V6 M5 14 H6 M5 19 H6", Violet, 2)],
            [new(21, 23, 2.3, Green)]),
        ("board-gate", 16) => new(name, size, 3,
            [new("M4 5 H8 Q11 5 12 8 Q11 11 8 11 H4", Cyan, 1.8), new("M3 7 V9", Violet, 1.5)],
            [new(13, 8, 1.4, Green)]),
        ("board-gate", 24) => new(name, size, 5,
            [new("M6 7 H11 Q16 7 18 12 Q16 17 11 17 H6", Cyan, 2.7), new("M4 11 H8 M4 14 H8", Violet, 1.5)],
            [new(20, 12, 1.9, Green)]),
        ("board-gate", 32) => new(name, size, 6,
            [new("M8 10 H15 Q22 10 24 16 Q22 22 15 22 H8", Cyan, 3), new("M5 14 H11 M5 18 H11 M15 5 V7", Violet, 2)],
            [new(27, 16, 2.3, Green)]),
        ("pulse-loop", 16) => new(name, size, 3,
            [new("M4 5 L6 3 H10 L12 5 M4 11 L6 13 H10 L12 11", Violet, 1.5),
             new("M3 8 H5 L7 6 L9 10 L11 8 H13", Cyan, 1.5)], [new(13, 8, 1.25, Green)]),
        ("pulse-loop", 24) => new(name, size, 5,
            [new("M6 8 L9 5 H15 L18 8 M6 16 L9 19 H15 L18 16", Violet, 2.1),
             new("M4 12 H8 L10 9 L13 15 L16 12 H20", Cyan, 2.2)], [new(20, 12, 1.8, Green)]),
        ("pulse-loop", 32) => new(name, size, 6,
            [new("M7 12 V9 L11 6 H21 L25 10 M7 21 L11 25 H21 L25 21", Violet, 2.6),
             new("M5 17 H10 L14 12 L17 21 L21 16 H27", Cyan, 2.8)], [new(27, 16, 1.9, Green)]),
        _ => throw new ArgumentException($"Неизвестный вариант {name} / {size}.")
    };
}
