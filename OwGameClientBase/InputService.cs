using System;
using System.Numerics;

namespace OW.Game.Client
{
    /// <summary>
    /// 主键盘区 + 常用控制键 的按键状态掩码。
    /// 使用 ulong 作为 bitmask，最多 64 个键位。
    /// </summary>
    [Flags]
    public enum KeyboardMainState : ulong
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
    /// 小键盘区 + 功能键 的按键状态掩码。
    /// 使用第二个 bitmask，避免单个 ulong 不够用。
    /// </summary>
    [Flags]
    public enum KeyboardExtraState : ulong
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

        // 多媒体键（15）
        MediaPlayPause = 1UL << 28,      // 播放/暂停
        MediaStop = 1UL << 29,           // 停止
        MediaPrevious = 1UL << 30,       // 上一曲
        MediaNext = 1UL << 31,           // 下一曲
        VolumeUp = 1UL << 32,            // 音量增加
        VolumeDown = 1UL << 33,          // 音量减少
        VolumeMute = 1UL << 34,          // 静音
        BrowserBack = 1UL << 35,         // 浏览器后退
        BrowserForward = 1UL << 36,      // 浏览器前进
        BrowserRefresh = 1UL << 37,      // 浏览器刷新
        BrowserHome = 1UL << 38,         // 浏览器主页
        LaunchMail = 1UL << 39,          // 启动邮件
        LaunchMediaSelect = 1UL << 40,   // 启动媒体选择器
        LaunchApp1 = 1UL << 41,          // 启动应用1
        LaunchApp2 = 1UL << 42,          // 启动应用2

        // 预留扩展位（21个，足够未来扩展）
        Reserved1 = 1UL << 43,
        Reserved2 = 1UL << 44,
        Reserved3 = 1UL << 45,
        Reserved4 = 1UL << 46,
        Reserved5 = 1UL << 47,
        Reserved6 = 1UL << 48,
        Reserved7 = 1UL << 49,
        Reserved8 = 1UL << 50,
        Reserved9 = 1UL << 51,
        Reserved10 = 1UL << 52,
        Reserved11 = 1UL << 53,
        Reserved12 = 1UL << 54,
        Reserved13 = 1UL << 55,
        Reserved14 = 1UL << 56,
        Reserved15 = 1UL << 57,
        Reserved16 = 1UL << 58,
        Reserved17 = 1UL << 59,
        Reserved18 = 1UL << 60,
        Reserved19 = 1UL << 61,
        Reserved20 = 1UL << 62,
        Reserved21 = 1UL << 63,
    }

    /// <summary>
    /// 输入服务：记录当前帧输入设备的原始状态。
    /// 
    /// 设计定位：
    /// - 这是一个共享服务，供所有 ECS 系统读取输入状态
    /// - 不是标准 ECS 组件，不参与 ECS 批处理
    /// - UI 层（Blazor）负责写入状态，ECS 系统只读取
    /// 
    /// v1.4.0 重大改进：
    /// - 字段私有化，通过 BeginFrame() 统一设置
    /// - BeginFrame() 自动保存上一帧状态
    /// - 新增鼠标中键支持（三键鼠标）
    /// </summary>
    public sealed class InputService
    {
        #region 当前帧状态（私有字段，通过 BeginFrame 设置）

        /// <summary>
        /// 主键盘区 + 常用控制键 的按键状态（当前帧）。
        /// </summary>
        private KeyboardMainState _keyboardMain;

        /// <summary>
        /// 小键盘 + 功能键 的按键状态（当前帧）。
        /// </summary>
        private KeyboardExtraState _keyboardExtra;

        /// <summary>
        /// 鼠标屏幕坐标（由 BeginFrame 设置）。
        /// </summary>
        private Vector2 _mousePosition;

        /// <summary>
        /// 鼠标移动量（由 BeginFrame 设置）。
        /// </summary>
        private Vector2 _mouseDelta;

        /// <summary>
        /// 鼠标左键是否按下（当前帧）。
        /// </summary>
        private bool _mouseLeftPressed;

        /// <summary>
        /// 鼠标右键是否按下（当前帧）。
        /// </summary>
        private bool _mouseRightPressed;

        /// <summary>
        /// 鼠标中键是否按下（当前帧）。
        /// </summary>
        private bool _mouseMiddlePressed;

        /// <summary>
        /// 虚拟摇杆方向（手机端），分量的范围 [-1,1]。
        /// </summary>
        private Vector2 _joystickDirection;

        #endregion

        #region 上一帧状态（自动缓存）

        /// <summary>
        /// 主键盘区的上一帧状态。
        /// </summary>
        public KeyboardMainState PreviousKeyboardMain { get; private set; }

        /// <summary>
        /// 扩展键区的上一帧状态。
        /// </summary>
        public KeyboardExtraState PreviousKeyboardExtra { get; private set; }

        /// <summary>
        /// 鼠标左键的上一帧状态。
        /// </summary>
        public bool PreviousMouseLeftPressed { get; private set; }

        /// <summary>
        /// 鼠标右键的上一帧状态。
        /// </summary>
        public bool PreviousMouseRightPressed { get; private set; }

        /// <summary>
        /// 鼠标中键的上一帧状态。
        /// </summary>
        public bool PreviousMouseMiddlePressed { get; private set; }

        #endregion

        #region 公开只读属性（供 ECS 系统读取）

        /// <summary>
        /// 当前帧鼠标位置（只读）。
        /// </summary>
        public Vector2 MousePosition => _mousePosition;

        /// <summary>
        /// 当前帧鼠标移动量（只读）。
        /// </summary>
        public Vector2 MouseDelta => _mouseDelta;

        /// <summary>
        /// 当前帧虚拟摇杆方向（只读）。
        /// </summary>
        public Vector2 JoystickDirection => _joystickDirection;

        #endregion

        #region 按键状态检测（简洁命名）

        /// <summary>
        /// 判断主键盘某键是否按下（当前帧）。
        /// </summary>
        public bool IsDown(KeyboardMainState key) => (_keyboardMain & key) != 0;

        /// <summary>
        /// 判断扩展键（小键盘/功能键）是否按下（当前帧）。
        /// </summary>
        public bool IsDown(KeyboardExtraState key) => (_keyboardExtra & key) != 0;

        /// <summary>
        /// 判断主键盘某键是否未按下（当前帧）。
        /// </summary>
        public bool IsUp(KeyboardMainState key) => (_keyboardMain & key) == 0;

        /// <summary>
        /// 判断扩展键是否未按下（当前帧）。
        /// </summary>
        public bool IsUp(KeyboardExtraState key) => (_keyboardExtra & key) == 0;

        /// <summary>
        /// 判断鼠标左键是否按下（当前帧）。
        /// </summary>
        public bool IsMouseLeftDown() => _mouseLeftPressed;

        /// <summary>
        /// 判断鼠标右键是否按下（当前帧）。
        /// </summary>
        public bool IsMouseRightDown() => _mouseRightPressed;

        /// <summary>
        /// 判断鼠标中键是否按下（当前帧）。
        /// </summary>
        public bool IsMouseMiddleDown() => _mouseMiddlePressed;

        #endregion

        #region 边缘检测（刚按下/刚释放）

        /// <summary>
        /// 判断主键盘某键是否刚按下（本帧按下，上一帧未按）。
        /// 用于触发一次性动作，如跳跃、发射。
        /// </summary>
        public bool WasPressed(KeyboardMainState key)
        {
            return (_keyboardMain & key) != 0 && (PreviousKeyboardMain & key) == 0;
        }

        /// <summary>
        /// 判断主键盘某键是否刚释放（本帧未按，上一帧按下）。
        /// 用于检测按键释放事件，如蓄力攻击释放。
        /// </summary>
        public bool WasReleased(KeyboardMainState key)
        {
            return (_keyboardMain & key) == 0 && (PreviousKeyboardMain & key) != 0;
        }

        /// <summary>
        /// 判断扩展键是否刚按下（本帧按下，上一帧未按）。
        /// </summary>
        public bool WasPressed(KeyboardExtraState key)
        {
            return (_keyboardExtra & key) != 0 && (PreviousKeyboardExtra & key) == 0;
        }

        /// <summary>
        /// 判断扩展键是否刚释放（本帧未按，上一帧按下）。
        /// </summary>
        public bool WasReleased(KeyboardExtraState key)
        {
            return (_keyboardExtra & key) == 0 && (PreviousKeyboardExtra & key) != 0;
        }

        /// <summary>
        /// 判断鼠标左键是否刚按下。
        /// </summary>
        public bool WasMouseLeftPressed()
        {
            return _mouseLeftPressed && !PreviousMouseLeftPressed;
        }

        /// <summary>
        /// 判断鼠标左键是否刚释放。
        /// </summary>
        public bool WasMouseLeftReleased()
        {
            return !_mouseLeftPressed && PreviousMouseLeftPressed;
        }

        /// <summary>
        /// 判断鼠标右键是否刚按下。
        /// </summary>
        public bool WasMouseRightPressed()
        {
            return _mouseRightPressed && !PreviousMouseRightPressed;
        }

        /// <summary>
        /// 判断鼠标右键是否刚释放。
        /// </summary>
        public bool WasMouseRightReleased()
        {
            return !_mouseRightPressed && PreviousMouseRightPressed;
        }

        /// <summary>
        /// 判断鼠标中键是否刚按下。
        /// </summary>
        public bool WasMouseMiddlePressed()
        {
            return _mouseMiddlePressed && !PreviousMouseMiddlePressed;
        }

        /// <summary>
        /// 判断鼠标中键是否刚释放。
        /// </summary>
        public bool WasMouseMiddleReleased()
        {
            return !_mouseMiddlePressed && PreviousMouseMiddlePressed;
        }

        #endregion

        #region 帧管理（每帧调用一次，由 UI 层调用）

        /// <summary>
        /// 开始新的一帧，设置当前帧的输入状态并自动保存上一帧状态。
        /// 
        /// 调用时机：
        /// - 在游戏循环开始时调用
        /// - 在 UI 层采集完输入后立即调用
        /// - 在 ECS 系统读取输入之前调用
        /// 
        /// 自动操作：
        /// - 将当前帧状态保存为上一帧
        /// - 设置新的当前帧状态
        /// </summary>
        /// <param name="keyboardMain">新的主键盘区状态</param>
        /// <param name="keyboardExtra">新的扩展键区状态</param>
        /// <param name="mousePosition">新的鼠标位置</param>
        /// <param name="mouseDelta">新的鼠标移动量</param>
        /// <param name="mouseLeftPressed">鼠标左键是否按下</param>
        /// <param name="mouseRightPressed">鼠标右键是否按下</param>
        /// <param name="mouseMiddlePressed">鼠标中键是否按下</param>
        /// <param name="joystickDirection">虚拟摇杆方向（可选）</param>
        public void BeginFrame(
            KeyboardMainState keyboardMain,
            KeyboardExtraState keyboardExtra,
            Vector2 mousePosition,
            Vector2 mouseDelta,
            bool mouseLeftPressed,
            bool mouseRightPressed,
            bool mouseMiddlePressed,
            Vector2 joystickDirection = default)
        {
            // 1. 保存当前帧状态为上一帧
            PreviousKeyboardMain = _keyboardMain;
            PreviousKeyboardExtra = _keyboardExtra;
            PreviousMouseLeftPressed = _mouseLeftPressed;
            PreviousMouseRightPressed = _mouseRightPressed;
            PreviousMouseMiddlePressed = _mouseMiddlePressed;

            // 2. 设置新的当前帧状态
            _keyboardMain = keyboardMain;
            _keyboardExtra = keyboardExtra;
            _mousePosition = mousePosition;
            _mouseDelta = mouseDelta;
            _mouseLeftPressed = mouseLeftPressed;
            _mouseRightPressed = mouseRightPressed;
            _mouseMiddlePressed = mouseMiddlePressed;
            _joystickDirection = joystickDirection;
        }

        /// <summary>
        /// 重置所有输入状态为默认值。
        /// 通常在窗口失去焦点或场景切换时调用。
        /// </summary>
        public void Reset()
        {
            _keyboardMain = KeyboardMainState.None;
            _keyboardExtra = KeyboardExtraState.None;
            _mousePosition = Vector2.Zero;
            _mouseDelta = Vector2.Zero;
            _mouseLeftPressed = false;
            _mouseRightPressed = false;
            _mouseMiddlePressed = false;
            _joystickDirection = Vector2.Zero;

            PreviousKeyboardMain = KeyboardMainState.None;
            PreviousKeyboardExtra = KeyboardExtraState.None;
            PreviousMouseLeftPressed = false;
            PreviousMouseRightPressed = false;
            PreviousMouseMiddlePressed = false;
        }

        #endregion
    }
}
