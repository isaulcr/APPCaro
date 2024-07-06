using Microsoft.Maui.ApplicationModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace APPCaro
{
    public partial class MainPage : ContentPage
    {
        private PesFile.PesFile design;
        private float designScale = 1.0f;
        private int designRotation = 0;
        private Bitmap DrawArea;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void SelectBarcode(object sender, EventArgs e)
        {

            var images = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = ".PES Archivo",
            });

            if (images == null)
            {
                return;
            }

            var imageSource = images.FullPath.ToString();

            var imagen = await UpdateDesignImage(imageSource);
            ImageConverter converter = new ImageConverter();
            var res = (byte[])converter.ConvertTo(imagen, typeof(byte[]));
            barcodeImage.Source = ImageSource.FromStream(() => new MemoryStream(res)); // Asignar el bitmap a la imagen
         
        }


        private async Task<Bitmap> UpdateDesignImage(string imagen)
        {

            design = new PesFile.PesFile(imagen);

            if (design == null)
            {
                // No design loaded - nothing to update
                return null;
            }

            // Assume 96 DPI until we can come up with a better way to calculate it
            float screenDPI = 96;

            Bitmap tempImage = design.DesignToBitmap((float)0.1, (false), 1.0f, (screenDPI / design.NativeDPI));

            // Rotar imagen
            switch (designRotation)
            {
                case 90:
                    tempImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                case 180:
                    tempImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                case 270:
                    tempImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
            }

            // Escalar imagen
            if (true)
            {
                // Calculate size of image based on available drawing area
                float windowWidth = 400 - 3;
                float windowHeight = 400 - 3;

                // Figure out which dimension is more constrained
                float widthScale = windowWidth / tempImage.Width;
                float heightScale = windowHeight / tempImage.Height;
                if (widthScale < heightScale)
                {
                    designScale = widthScale;
                }
                else
                {
                    designScale = heightScale;
                }
            }

            int width = (int)(tempImage.Width * designScale);
            int height = (int)(tempImage.Height * designScale);

            if (width < 1 || height < 1)
            {
                // Image area is too small to update
                return null;
            }

            if (width != tempImage.Width || height != tempImage.Height)
            {
                // Scale image code from http://stackoverflow.com/questions/1922040/resize-an-image-c-sharp
                Rectangle destRect = new Rectangle(0, 0, width, height);
                Bitmap scaledImage = new Bitmap(width, height);

                scaledImage.SetResolution(tempImage.HorizontalResolution, tempImage.VerticalResolution);

                using (var graphics = Graphics.FromImage(scaledImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(tempImage, destRect, 0, 0, tempImage.Width, tempImage.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }
                // Keep the scaled image and dispose the intermediate image
                tempImage = scaledImage;
            }

            // About to abandon the current DrawArea object, dispose it now
            if (DrawArea != null)
            {
                DrawArea = null;
            }

            // Add transparency grid
            bool transparencia = true;
            if (transparencia)
            {
                DrawArea = new Bitmap(tempImage.Width, tempImage.Height);
                using (Graphics g = Graphics.FromImage(DrawArea))
                {
                    System.Drawing.Color gridColor = System.Drawing.Color.Black;
                    using (Pen gridPen = new Pen(gridColor))
                    {
                        int gridSize = 5;
                        for (int xStart = 0; (xStart * gridSize) < DrawArea.Width; xStart++)
                        {
                            for (int yStart = 0; (yStart * gridSize) < DrawArea.Height; yStart++)
                            {
                                // Fill even columns in even rows and odd columns in odd rows
                                if ((xStart % 2) == (yStart % 2))
                                {
                                    g.FillRectangle(gridPen.Brush, (xStart * gridSize), (yStart * gridSize), gridSize, gridSize);
                                }
                            }
                        }
                    }

                    g.DrawImage(tempImage, 0, 0);

                    return (tempImage);
                    // Done with tempImage
                }
            }
            else
            {
                // Keeping the object tempImage was pointing at, so don't dispose it
                DrawArea = tempImage;
                return (tempImage);

            }
        }


    }

}
