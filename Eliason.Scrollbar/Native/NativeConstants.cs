#region

#endregion

namespace Eliason.Scrollbar.Native
{
    public static class NativeConstants
    {
        public const uint SRCAND = 0x008800C6; /* dest = source AND dest */
        public const uint SRCCOPY = 0x00CC0020; /* dest = source  */
        public const uint SRCERASE = 0x00440328; /* dest = source AND (NOT dest ) */
        public const uint SRCINVERT = 0x00660046; /* dest = source XOR dest */
        public const uint SRCPAINT = 0x00EE0086; /* dest = source OR dest */
    }
}