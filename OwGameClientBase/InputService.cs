using System;
using System.Numerics;

namespace OW.Game.Client
{
    /// <summary>
    /// 主键盘区 + 常用控制键 的按键掩码。
    /// 使用 ulong 作为 bitmask，最多 64 个键位。
    /// </summary>
    [Flags]
    public enum KeyboardKeyMain : ulong
    {
        None = 0,

        // 字母键 A-Z（26）
        A = 1UL << 0,
        B = 1UL << 1,
        C = 1UL << 2,
        D = 1UL << 3,
        E = 1UL << 4,
        F = 1UL << 5,
        G = 1UL << 6,
        H = 1UL << 7,
        I = 1UL << 8,
        J = 1UL << 9,
        K = 1UL << 10,
        L = 1UL << 11,
        M = 1UL << 12,
        N = 1UL << 13,
        O = 1UL << 14,
        P = 1UL << 15,
        Q = 1UL << 16,
        R = 1UL << 17,
        S = 1UL << 18,
        T = 1UL << 19,
        U = 1UL << 20,
        V = 1UL << 21,
        W = 1UL << 22,
        X = 1UL << 23,
        Y = 1UL << 24,
        Z = 1UL << 25,

        // 数字键 0-9（主键盘）（10）
        D0 = 1UL << 26,
        D1 = 1UL << 27,
        D2 = 1UL << 28,
        D3 = 1UL << 29,
        D4 = 1UL << 30,
        D5 = 1UL << 31,
        D6 = 1UL << 32,
        D7 = 1UL << 33,
        D8 = 1UL << 34,
        D9 = 1UL << 35,

        // 方向键（4）
        ArrowUp = 1UL << 36,
        ArrowDown = 1UL << 37,
        ArrowLeft = 1UL << 38,
        ArrowRight = 1UL << 39,

        // 常用控制键（8）
        Escape = 1UL << 40,
        Tab = 1UL << 41,
        CapsLock = 1UL << 42,
        Shift = 1UL << 43,   // 不区分左右
        Ctrl = 1UL << 44,   // 不区分左右
        Alt = 1UL << 45,   // 不区分左右
        Space = 1UL << 46,
        Enter = 1UL << 47,
        Backspace = 1UL << 48,
        Insert = 1UL << 49,
        Delete = 1UL << 50,
        Home = 1UL << 51,
        End = 1UL << 52,
        PageUp = 1UL << 53,
        PageDown = 1UL << 54,

        // 锁定键（3）
        NumLock = 1UL << 55,
        ScrollLock = 1UL << 56,

        // 预留扩展位
        Reserved1 = 1UL << 57,
        Reserved2 = 1UL << 58,
        Reserved3 = 1UL << 59,
        Reserved4 = 1UL << 60,
        Reserved5 = 1UL << 61,
        Reserved6 = 1UL << 62,
        Reserved7 = 1UL << 63,
    }

    /// <summary>
    /// 小键盘区 + 功能键 的按键掩码。
    /// 使用第二个 bitmask，避免单个 ulong 不够用。
    /// </summary>
    [Flags]
    public enum KeyboardKeyExtra : ulong
    {
        None = 0,

        // 小键盘数字键 0-9（10）
        NumPad0 = 1UL << 0,
        NumPad1 = 1UL << 1,
        NumPad2 = 1UL << 2,
        NumPad3 = 1UL << 3,
        NumPad4 = 1UL << 4,
        NumPad5 = 1UL << 5,
        NumPad6 = 1UL << 6,
        NumPad7 = 1UL << 7,
        NumPad8 = 1UL << 8,
        NumPad9 = 1UL << 9,

        // 小键盘运算键（6）
        NumPadAdd = 1UL << 10,
        NumPadSubtract = 1UL << 11,
        NumPadMultiply = 1UL << 12,
        NumPadDivide = 1UL << 13,
        NumPadDecimal = 1UL << 14,
        NumPadEnter = 1UL << 15,

        // 功能键 F1-F12（12）
        F1 = 1UL << 16,
        F2 = 1UL << 17,
        F3 = 1UL << 18,
        F4 = 1UL << 19,
        F5 = 1UL << 20,
        F6 = 1UL << 21,
        F7 = 1UL << 22,
        F8 = 1UL << 23,
        F9 = 1UL << 24,
        F10 = 1UL << 25,
        F11 = 1UL << 26,
        F12 = 1UL << 27,

        // 预留扩展位
        Reserved1 = 1UL << 28,
        Reserved2 = 1UL << 29,
        Reserved3 = 1UL << 30,
        Reserved4 = 1UL << 31,
        Reserved5 = 1UL << 32,
        Reserved6 = 1UL << 33,
        Reserved7 = 1UL << 34,
        Reserved8 = 1UL << 35,
        Reserved9 = 1UL << 36,
        Reserved10 = 1UL << 37,
        Reserved11 = 1UL << 38,
        Reserved12 = 1UL << 39,
        Reserved13 = 1UL << 40,
        Reserved14 = 1UL << 41,
        Reserved15 = 1UL << 42,
        Reserved16 = 1UL << 43,
        Reserved17 = 1UL << 44,
        Reserved18 = 1UL << 45,
        Reserved19 = 1UL << 46,
        Reserved20 = 1UL << 47,
        Reserved21 = 1UL << 48,
        Reserved22 = 1UL << 49,
        Reserved23 = 1UL << 50,
        Reserved24 = 1UL << 51,
        Reserved25 = 1UL << 52,
        Reserved26 = 1UL << 53,
        Reserved27 = 1UL << 54,
        Reserved28 = 1UL << 55,
        Reserved29 = 1UL << 56,
        Reserved30 = 1UL << 57,
        Reserved31 = 1UL << 58,
        Reserved32 = 1UL << 59,
        Reserved33 = 1UL << 60,
        Reserved34 = 1UL << 61,
        Reserved35 = 1UL << 62,
        Reserved36 = 1UL << 63,
    }

    /// <summary>
    /// 输入服务：记录当前帧输入设备的原始状态。
    /// 不解释输入，不参与 ECS 批处理。
    /// UI 层负责写入，其他服务负责读取并解释。
    /// </summary>
    public sealed class InputService
    {
        /// <summary>
        /// 主键盘区 + 常用控制键 的按键状态。
        /// </summary>
        public KeyboardKeyMain KeyboardMain;

        /// <summary>
        /// 小键盘 + 功能键 的按键状态。
        /// </summary>
        public KeyboardKeyExtra KeyboardExtra;

        /// <summary>
        /// 鼠标屏幕坐标（由 UI 层写入）。
        /// </summary>
        public Vector2 MousePosition;

        /// <summary>
        /// 鼠标移动量（由 UI 层写入）。
        /// </summary>
        public Vector2 MouseDelta;

        /// <summary>
        /// 鼠标左键是否按下。
        /// </summary>
        public bool MouseLeftPressed;

        /// <summary>
        /// 鼠标右键是否按下。
        /// </summary>
        public bool MouseRightPressed;

        /// <summary>
        /// 虚拟摇杆方向（手机端），分量的范围 [-1,1]。
        /// </summary>
        public Vector2 JoystickDirection;

        /// <summary>
        /// 判断主键盘某键是否按下。
        /// </summary>
        public bool IsMainPressed(KeyboardKeyMain key) => (KeyboardMain & key) != 0;

        /// <summary>
        /// 判断扩展键（小键盘/功能键）是否按下。
        /// </summary>
        public bool IsExtraPressed(KeyboardKeyExtra key) => (KeyboardExtra & key) != 0;

        /// <summary>
        /// 每帧结束时可调用，用于清空瞬时输入（如 MouseDelta）。
        /// 键盘状态通常由 UI 层按 KeyDown/KeyUp 维护，不在此清空。
        /// </summary>
        public void ClearFrame()
        {
            MouseDelta = Vector2.Zero;
            // JoystickDirection 通常每帧重写，无需在此清空。
        }
    }
}
