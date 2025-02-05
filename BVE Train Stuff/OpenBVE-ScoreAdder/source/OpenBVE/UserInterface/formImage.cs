using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OpenBve {
    internal partial class formImage : Form {
	    private formImage() {
            InitializeComponent();
        }

        // show image dialog
        internal static void ShowImageDialog(Image Image) {
            formImage Dialog = new formImage {CurrentImage = Image};
            Dialog.ShowDialog();
            Dialog.Dispose();
        }

        // members
	    private Image CurrentImage;

        // resize
        private void formImage_Resize(object sender, EventArgs e) {
            Invalidate();
        }

        // key down
        private void formImage_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Escape) {
                Close();
            }
        }

        // paint
        private void formImage_Paint(object sender, PaintEventArgs e) {
            float aw = ClientRectangle.Width;
            float ah = ClientRectangle.Height;
            float ar = aw / ah;
            float bw = CurrentImage.Width;
            float bh = CurrentImage.Height;
            float br = bw / bh;
            try
            {
	            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
	            LinearGradientBrush Gradient = new LinearGradientBrush(new Point(0, 0), new Point(0, ClientRectangle.Height), SystemColors.Control, SystemColors.ControlDark);
	            e.Graphics.FillRectangle(Gradient, 0, 0, ClientRectangle.Width, ClientRectangle.Height);
	            e.Graphics.DrawImage(CurrentImage, ar > br ? new RectangleF(0.5f * (aw - ah * br), 0.0f, ah * br, ah) : new RectangleF(0.0f, 0.5f * (ah - aw / br), aw, aw / br), new RectangleF(0.0f, 0.0f, bw, bh), GraphicsUnit.Pixel);
            }
            catch
            {
				// Ignored
            }
        }

    }
}