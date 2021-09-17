using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Windows_Folder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            InitializeComponent();

            this.EnableBlur();
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.FromArgb(103, 65, 114);
            TransparencyKey = Color.FromArgb(103, 65, 114);
        }
        Point pos;
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(103, 65, 114)), this.ClientRectangle);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var sb = new SolidBrush(Color.FromArgb(100, 100, 100, 100));
            e.Graphics.FillRectangle(sb, this.DisplayRectangle);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            int X = Cursor.Position.X - Width / 2;
            int Y = Cursor.Position.Y - Height / 2;
            Location = new Point(X, Y);
            load();;
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            Save();
            Application.Exit();
        }
        private void Form1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                if (File.Exists(path))
                {
                    Icon appIcon = Icon.ExtractAssociatedIcon(path);

                    InitializePB(e, appIcon, path);
                }
            }
        }
        private void Box_DoubleClick(object sender, EventArgs e)
        {
            Control box = sender as Control;
            Process.Start($"{box.Tag}");
            Save();

            Application.Exit();         
        }
        private void Box_MouseMove(object sender, MouseEventArgs e)
        {
            PictureBox box = sender as PictureBox;
            if (e.Button == MouseButtons.Left)
            {
                Point coord = new Point(Cursor.Position.X - pos.X, Cursor.Position.Y - pos.Y);
                box.Location = PointToClient(coord);
            }
            CorrectPosition(sender);
        }
        private void Box_MouseDown(object sender, MouseEventArgs e)
        {
            pos = new Point(e.X, e.Y);
        }
        private void Box_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                PictureBox box = sender as PictureBox;
                Controls.Remove(box);
            }
        }
        private void Box_MouseUp(object sender, MouseEventArgs e)
        {
            CorrectPosition(sender);
        }

        private void CorrectPosition(object sender)
        {
            var box = sender as PictureBox;
            int X = box.Location.X;
            int Y = box.Location.Y;
            int W = box.Width;
            int H = box.Height;
            if (X < 0) box.Location = new Point(0, Y);
            if (Y < 0) box.Location = new Point(X, 0);
            if (X + W > Width) box.Location = new Point(Width - W, Y);
            if (Y + H > Height) box.Location = new Point(X, Height - H);
        }

        private void InitializePB(DragEventArgs e, Icon appIcon, string path)
        {
            PictureBox box = new PictureBox
            {
                Location = PointToClient(new Point(e.X - appIcon.Width / 2, e.Y - appIcon.Height / 2)),
                Size = new Size(appIcon.Width, appIcon.Height),
                Image = appIcon.ToBitmap(),
                Tag = path
            };
            box.DoubleClick += Box_DoubleClick;
            box.MouseDown += Box_MouseDown;
            box.MouseMove += Box_MouseMove;
            box.MouseClick += Box_MouseClick;
            box.MouseUp += Box_MouseUp;
            Controls.Add(box);
        }
        private void InitializePB(Item item)
        {
            PictureBox box = new PictureBox
            {
                Location = item.Location,
                Size = item.Size,
                Image = item.Image,
                Tag = item.Tag
            };
            box.DoubleClick += Box_DoubleClick;
            box.MouseDown += Box_MouseDown;
            box.MouseMove += Box_MouseMove;
            box.MouseClick += Box_MouseClick;
            box.MouseUp += Box_MouseUp;
            Controls.Add(box);
        }

        private void load()
        {
            string path = GetPath();
            object boxes = null;
            if (File.Exists(path))
            {
                byte[] load = File.ReadAllBytes(path);
                
                BinaryFormatter bf = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream(load)) {
                    try { boxes = bf.Deserialize(ms); }
                    catch (Exception) { }
                }
            }

            if (boxes == null) return;
            foreach (var item in boxes as Item[])
            {
                InitializePB(item);
            }
        }
        private void Save()
        {
            string path = GetPath();
            Item[] boxes= PictureBoxToItem();
            
            byte[] bytes = null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, boxes);
                bytes = ms.ToArray();
            }
            File.WriteAllBytes(path, bytes);
            
        }

        private string GetPath()
        {
            string directory = Application.CommonAppDataPath;
            return Path.Combine(directory, "store.txt");
        }

        private Item[] PictureBoxToItem()
        {
            List<Item> temp = new List<Item>();
            foreach (var item in Controls)
            {
                if (item.GetType() == typeof(PictureBox))
                {
                    var x = item as PictureBox;
                    temp.Add(new Item(x.Location, x.Size, x.Image, x.Tag));
                }
            }
            return temp.ToArray();
        }
    }

    [Serializable]
    class Item
    {
        public Point Location;
        public Size Size;
        public Image Image;
        public object Tag;

        public Item(Point Location, Size Size, Image Image, object Tag)
        {
            this.Location = Location;
            this.Size = Size;
            this.Image = Image;
            this.Tag = Tag;
        }
    }
    public static class WindowExtension
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static internal extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public static void EnableBlur(this Form @this)
        {
            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var Data = new WindowCompositionAttributeData();
            Data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            Data.SizeOfData = accentStructSize;
            Data.Data = accentPtr;
            SetWindowCompositionAttribute(@this.Handle, ref Data);
            Marshal.FreeHGlobal(accentPtr);
        }

    }
    enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_INVALID_STATE = 4
    }

    struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }
}
