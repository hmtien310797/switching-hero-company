namespace Immortal_Switch.Scripts.Tutorial
{
    /// <summary>
    /// Chứa các hằng số định nghĩa loại hành động của từng bước hướng dẫn (Tutorial).
    /// Giá trị phải đồng bộ với dữ liệu trong file CSV/Excel.
    /// </summary>
    public static class TutorialConstants
    {
        /// <summary>
        /// Hiển thị hội thoại hoặc nội dung hướng dẫn cho người chơi.
        /// </summary>
        public const string DIALOGUE = "Dialogue";

        /// <summary>
        /// Làm nổi bật một đối tượng và yêu cầu người chơi nhấn vào đối tượng đó
        /// để hoàn thành bước hướng dẫn.
        /// </summary>
        public const string FOCUS_CLICK = "FocusClick";

        /// <summary>
        /// Yêu cầu người chơi nhấn nút Tiếp tục để chuyển sang bước hướng dẫn tiếp theo.
        /// </summary>
        public const string TAP_CONTINUE = "TapContinue";

        /// <summary>
        /// Yêu cầu người chơi thực hiện thao tác kéo hoặc nhấn vào đối tượng được chỉ định.
        /// Chỉ cần một trong hai thao tác thành công là hoàn thành bước hướng dẫn.
        /// </summary>
        public const string DRAG_OR_CLICK = "DragOrClick";
    }

    public static class TutorialGuideIds
    {
        /// <summary>
        /// huong dan dau game cho new user
        /// </summary>
        public const int NEW_USER_GUIDE = 1;
    }
}