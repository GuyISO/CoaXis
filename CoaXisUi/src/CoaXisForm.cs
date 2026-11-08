using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>Windowsのサイズ変更枠を維持し、タイトルバーをCoaXisヘッダーに置き換えるフォーム。</summary>
public class CoaXisForm : Form
{
    private const int WmNcCalcSize = 0x0083;
    private const int WmNcHitTest = 0x0084;
    private const int WmNcMouseMove = 0x00A0;
    private const int WmNcLeftButtonDown = 0x00A1;
    private const int WmNcMouseLeave = 0x02A2;
    private const int WmNcLeftButtonUp = 0x00A2;
    private const int WmMouseMove = 0x0200;
    private const int WmLeftButtonUp = 0x0202;
    private const int WmCaptureChanged = 0x0215;
    private const int WmCancelMode = 0x001F;
    private const int WmSysCommand = 0x0112;
    private const int HtClient = 1;
    private const int HtCaption = 2;
    private const int HtMaxButton = 9;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int SmCxSizeFrame = 32;
    private const int SmCySizeFrame = 33;
    private const int SmCxPaddedBorder = 92;
    private const uint TmeLeave = 0x00000002;
    private const uint TmeNonClient = 0x00000010;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;
    private readonly CoaXisHeaderControl _header = new();
    private bool _maximizePressed;

    /// <summary>サイズ変更可能なフォームを作成する。</summary>
    public CoaXisForm()
    {
        FormBorderStyle = FormBorderStyle.Sizable;
        Controls.Add(_header);
        Resize += OnWindowResize;
    }

    /// <summary>フォーム上部の共通ヘッダーを取得する。</summary>
    public CoaXisHeaderControl Header => _header;

    /// <summary>共通ヘッダーに表示するアプリケーション名を取得または設定する。</summary>
    [DefaultValue("")]
    public string HeaderTitle
    {
        get => _header.ApplicationTitle;
        set => _header.ApplicationTitle = value;
    }

    /// <inheritdoc />
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (_header != null && e.Control != _header && _header.Parent == this)
        {
            _header.SendToBack();
        }
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoSize | SwpNoMove | SwpNoZOrder | SwpFrameChanged))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        UpdateHeader();
    }

    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmNcCalcSize && m.LParam != IntPtr.Zero)
        {
            // 通常時は上端を描画領域に含め、最大化時は作業領域外の枠を避ける。
            NativeRect proposed = Marshal.PtrToStructure<NativeRect>(m.LParam);
            base.WndProc(ref m);
            NativeRect adjusted = Marshal.PtrToStructure<NativeRect>(m.LParam);
            adjusted.Top = proposed.Top + (IsZoomed(m.HWnd) ? GetResizeBorder(m.HWnd, SmCySizeFrame) : 0);
            Marshal.StructureToPtr(adjusted, m.LParam, false);
            m.Result = IntPtr.Zero;
            return;
        }

        if (m.Msg == WmNcHitTest)
        {
            base.WndProc(ref m);
            if (_header != null && _header.IsHandleCreated)
            {
                long coordinates = m.LParam.ToInt64();
                var point = new Point(unchecked((short)coordinates), unchecked((short)(coordinates >> 16)));
                if (_header.RectangleToScreen(_header.ClientRectangle).Contains(point))
                {
                    Rectangle bounds = _header.RectangleToScreen(_header.ClientRectangle);
                    int resizeHeight = GetResizeBorder(m.HWnd, SmCySizeFrame);
                    if (!IsZoomed(m.HWnd) && point.Y < bounds.Top + resizeHeight)
                    {
                        int resizeWidth = GetResizeBorder(m.HWnd, SmCxSizeFrame);
                        int hit = point.X < bounds.Left + resizeWidth ? HtTopLeft
                            : point.X >= bounds.Right - resizeWidth ? HtTopRight
                            : HtTop;
                        m.Result = new IntPtr(hit);
                    }
                    else
                    {
                        // 最大化領域を非クライアントとして認識させ、Windows 11のスナップレイアウトを有効にする。
                        m.Result = new IntPtr(_header.IsMaximizeButton(point) ? HtMaxButton : HtCaption);
                    }
                }
                else if (m.Result.ToInt32() == HtCaption)
                {
                    m.Result = new IntPtr(HtClient);
                }
            }

            return;
        }

        if (m.Msg == WmNcLeftButtonDown && m.WParam.ToInt32() == HtMaxButton && _header != null)
        {
            if (MaximizeBox)
            {
                _maximizePressed = true;
                Capture = true;
            }

            return;
        }

        if (_maximizePressed && (m.Msg == WmMouseMove || m.Msg == WmLeftButtonUp))
        {
            long coordinates = m.LParam.ToInt64();
            var clientPoint = new Point(unchecked((short)coordinates), unchecked((short)(coordinates >> 16)));
            bool overButton = _header.IsMaximizeButton(PointToScreen(clientPoint));
            _header.SetMaximizeHovered(overButton);
            if (m.Msg == WmLeftButtonUp)
            {
                _maximizePressed = false;
                Capture = false;
                if (overButton)
                {
                    _header.ToggleWindowState();
                }
            }

            return;
        }

        if (_maximizePressed && (m.Msg == WmCaptureChanged || m.Msg == WmCancelMode))
        {
            _maximizePressed = false;
            _header.SetMaximizeHovered(false);
        }

        if (m.Msg == WmNcMouseMove && _header != null)
        {
            _header.SetMaximizeHovered(m.WParam.ToInt32() == HtMaxButton);
            var tracking = new TrackMouseEventData
            {
                Size = (uint)Marshal.SizeOf<TrackMouseEventData>(),
                Flags = TmeLeave | TmeNonClient,
                Window = Handle
            };
            if (!TrackMouseEvent(ref tracking))
            {
                Trace.TraceError("タイトルバーのマウス追跡に失敗しました: {0}", new Win32Exception(Marshal.GetLastWin32Error()).Message);
            }
        }
        else if ((m.Msg == WmNcMouseLeave || m.Msg == WmNcLeftButtonUp || m.Msg == WmSysCommand) && _header != null)
        {
            _header.SetMaximizeHovered(false);
        }

        base.WndProc(ref m);
    }

    /// <inheritdoc />
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        UpdateHeader();
    }

    /// <inheritdoc />
    protected override void OnLoad(EventArgs e)
    {
        ThemeManager.Apply(this);
        base.OnLoad(e);
    }

    private void OnWindowResize(object sender, EventArgs e) => UpdateHeader();

    private void UpdateHeader()
    {
        _header.Padding = new Padding(0, IsHandleCreated && !IsZoomed(Handle) ? GetResizeBorder(Handle, SmCySizeFrame) : 0, 0, 0);
        _header.UpdateWindowState();
    }

    private static int GetResizeBorder(IntPtr window, int frameMetric)
    {
        uint dpi = GetDpiForWindow(window);
        return GetSystemMetricsForDpi(frameMetric, dpi) + GetSystemMetricsForDpi(SmCxPaddedBorder, dpi);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TrackMouseEventData
    {
        public uint Size;
        public uint Flags;
        public IntPtr Window;
        public uint HoverTime;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int index, uint dpi);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TrackMouseEvent(ref TrackMouseEventData tracking);
}
