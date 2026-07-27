namespace Immortal_Switch.Scripts.PlayerSystem.Models
{
    /// <summary>
    /// Mirror của nakama/src/config/game_rename_fee.js (server, nguồn sự thật) — bảng nhỏ và hiếm khi
    /// đổi nên hardcode ở client thay vì đi qua Excel Config Tool pipeline. Nếu server đổi bảng giá,
    /// phải tự cập nhật lại bảng này cho khớp.
    /// </summary>
    public static class RenameFeeConfig
    {
        // (attemptNumber, cost) — attemptNumber 1-indexed, không tính lần đặt tên lúc tạo account.
        private static readonly (int Time, int CostDiamond)[] Rows =
        {
            (1, 0),
            (2, 150),
            (3, 300),
            (4, 600),
            (5, 1200),
            (6, 2400),
            (7, 4800),
            (8, 4800),
            (9, 4800),
            (10, 4800),
        };

        /// <summary>Giá kim cương cho lần đổi tên thứ attemptNumber (1-indexed). Vượt quá dòng cuối
        /// trong bảng thì dùng lại giá của dòng cuối — khớp logic getRenameFee ở server.</summary>
        public static int GetFee(int attemptNumber)
        {
            foreach (var row in Rows)
                if (row.Time == attemptNumber) return row.CostDiamond;

            return Rows[Rows.Length - 1].CostDiamond;
        }
    }
}
