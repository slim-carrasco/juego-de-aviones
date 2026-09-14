using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http.Json;
using System.Windows.Forms;

namespace JuegoAviones
{
    public partial class Form1 : Form
    {
        // Controles de interfaz
        private StatusStrip statusStrip1 = new StatusStrip();
        private ToolStripStatusLabel toolStripStatusLabel1 = new ToolStripStatusLabel();
        private ToolStripStatusLabel toolStripStatusLabel2 = new ToolStripStatusLabel();

        // Variables globales
        PictureBox navex = new PictureBox();
        PictureBox naveRival = new PictureBox();
        PictureBox contiene = new PictureBox();
        System.Windows.Forms.Timer tiempo = new System.Windows.Forms.Timer();
        private static readonly HttpClient ClienteApi = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5080"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        int Dispara = 0;
        bool flag = false;
        bool partidaRegistrada = false;

        public Form1()
        {
            InitializeComponent();
            ConfigurarComponentes();
            Iniciar();
        }

        private void ConfigurarComponentes()
        {
            this.statusStrip1.Items.AddRange(new ToolStripItem[] {
                this.toolStripStatusLabel1,
                this.toolStripStatusLabel2
            });
            this.Controls.Add(this.statusStrip1);
        }

        // Crear misil
        public void CrearMisil(int AngRotar, Color pintar, string nombre, int x, int y)
        {
            PictureBox Balas = new PictureBox();
            int PosX = 1;
            int PosY = 1;
            int largoM = 11;
            int anchoM = 8;

            Point[] myMisil1 = {
                new Point(4 * PosX, 0 * PosY), new Point(5 * PosX, 1 * PosY),
                new Point(6 * PosX, 2 * PosY), new Point(6 * PosX, 7 * PosY),
                new Point(7 * PosX, 8 * PosY), new Point(8 * PosX, 9 * PosY),
                new Point(7 * PosX, 9 * PosY), new Point(6 * PosX, 10 * PosY),
                new Point(2 * PosX, 10 * PosY), new Point(1 * PosX, 9 * PosY),
                new Point(0 * PosX, 9 * PosY), new Point(1 * PosX, 8 * PosY),
                new Point(2 * PosX, 7 * PosY), new Point(2 * PosX, 2 * PosY),
                new Point(3 * PosX, 1 * PosY), new Point(4 * PosX, 0 * PosY)
            };

            Point[] myMisil = new Point[myMisil1.Length];
            for (int i = 0; i < myMisil1.Length; i++)
            {
                myMisil[i].X = myMisil1[i].X;
                if (AngRotar == 180)
                    myMisil[i].Y = largoM - myMisil1[i].Y;
                else
                    myMisil[i].Y = myMisil1[i].Y;
            }

            GraphicsPath ObjGrafico = new GraphicsPath();
            ObjGrafico.AddPolygon(myMisil);

            Balas.Location = new Point(x, y);
            Balas.BackColor = pintar;
            Balas.Size = new Size(anchoM * PosX, largoM * PosY);
            Balas.Region = new Region(ObjGrafico);
            Balas.Visible = true;
            Balas.Tag = nombre;

            Bitmap flagImg = new Bitmap(anchoM, largoM);
            Graphics flagImagen = Graphics.FromImage(flagImg);
            flagImagen.FillRectangle(Brushes.Orange, 2, 8, 5, 1);
            flagImagen.FillRectangle(Brushes.Yellow, 3, 10, 3, 1);
            Balas.Image = flagImg;

            contiene.Controls.Add(Balas);
        }

        // Bucle principal del juego
        private void ImpactarTick(object? sender, EventArgs e)
        {
            int X = naveRival.Location.X;
            int Y = naveRival.Location.Y;
            int X2 = navex.Location.X;
            int Y2 = navex.Location.Y;
            int W2 = navex.Width;
            int H2 = navex.Height;

            Dispara++;
            if (Dispara == 100 && naveRival.Visible)
            {
                int xRival = naveRival.Location.X + (naveRival.Width / 2);
                int yRival = naveRival.Location.Y + (naveRival.Height / 2);
                CrearMisil(180, Color.DarkRed, "Rival", xRival, yRival);
                Dispara = 0;
            }

            // Movimiento lateral del rival
            if (!flag)
            {
                if (contiene.Width <= naveRival.Location.X + naveRival.Width)
                    flag = true;
                else
                    naveRival.Left += 1;
            }
            else
            {
                if (naveRival.Location.X <= 0)
                    flag = false;
                else
                    naveRival.Left -= 1;
            }

            // Revisar proyectiles e impactos
            foreach (Control c in contiene.Controls)
            {
                if (c is PictureBox && c.Tag != null)
                {
                    int X1 = c.Location.X;
                    int Y1 = c.Location.Y;
                    int W1 = c.Width;
                    int H1 = c.Height;
                    string nombre = c.Tag?.ToString() ?? string.Empty;

                    // Impacto con Rival
                    if (X < X1 && X1 + W1 < X + naveRival.Width && Y < Y1 && Y1 + H1 < Y + naveRival.Height && nombre == "Misil")
                    {
                        c.Dispose();
                        int vidaRival = Convert.ToInt32(naveRival.Tag) - 10;
                        naveRival.Tag = vidaRival;
                        toolStripStatusLabel1.Text = "Vida del Rival: " + vidaRival;

                        if (vidaRival <= 0)
                        {
                            RegistrarResultadoEnServidor("Ganaste", Convert.ToInt32(navex.Tag), 0);
                            naveRival.Dispose();
                            Bitmap NuevoImg = new Bitmap(contiene.Width, contiene.Height);
                            Graphics flagImagen = Graphics.FromImage(NuevoImg);
                            flagImagen.DrawString("Felicitaciones Ganaste !", new Font("Arial", 16), Brushes.Blue, new Point(40, 150));
                            contiene.Image = NuevoImg;
                            tiempo.Stop();
                        }
                    }

                    // Impacto con Nave del Jugador
                    if (X2 < X1 && X1 + W1 < X2 + W2 && Y2 < Y1 && Y1 + H1 < Y2 + H2 && nombre == "Rival")
                    {
                        c.Dispose();
                        int miVida = Convert.ToInt32(navex.Tag) - 10;
                        navex.Tag = miVida;
                        toolStripStatusLabel2.Text = "Mi Nave: " + miVida;

                        if (miVida <= 0)
                        {
                            RegistrarResultadoEnServidor("Perdiste", 0, Convert.ToInt32(naveRival.Tag));
                            navex.Dispose();
                            Bitmap NuevoImg = new Bitmap(contiene.Width, contiene.Height);
                            Graphics flagImagen = Graphics.FromImage(NuevoImg);
                            flagImagen.DrawString("Perdiste el Juego", new Font("Arial", 16), Brushes.Red, new Point(70, 150));
                            contiene.Image = NuevoImg;
                            tiempo.Stop();
                        }
                    }

                    // Movimiento de balas
                    if (Y1 <= 0 && nombre == "Misil") c.Dispose();
                    if (Y1 >= contiene.Height && nombre == "Rival") c.Dispose();
                    if (nombre == "Misil") c.Top -= 10;
                    if (nombre == "Rival") c.Top += 10;
                }
            }
        }

        // Crear Naves
        public void CrearNave(PictureBox Avion, int AngRotar, int TipoX, Color Pintar, int Vida)
        {
            int largoN = 77;
            int anchoN = 58;

            Point[] myNave1 = {
                new Point(29, 0), new Point(30, 1), new Point(30, 6), new Point(31, 6),
                new Point(31, 11), new Point(32, 11), new Point(32, 17), new Point(35, 17),
                new Point(35, 16), new Point(37, 16), new Point(37, 17), new Point(38, 18),
                new Point(38, 28), new Point(39, 28), new Point(42, 39), new Point(45, 45),
                new Point(50, 51), new Point(51, 51), new Point(51, 52), new Point(58, 59),
                new Point(58, 66), new Point(35, 71), new Point(26, 77), new Point(23, 71),
                new Point(19, 71), new Point(14, 66), new Point(0, 66), new Point(7, 52),
                new Point(14, 45), new Point(16, 39), new Point(19, 28), new Point(20, 28),
                new Point(20, 18), new Point(21, 17), new Point(23, 17), new Point(26, 17),
                new Point(26, 11), new Point(28, 6), new Point(28, 1), new Point(29, 0)
            };

            GraphicsPath ObjGrafico = new GraphicsPath();
            ObjGrafico.AddPolygon(myNave1);

            Avion.BackColor = Pintar;
            Avion.Size = new Size(anchoN, largoN);
            Avion.Region = new Region(ObjGrafico);
            Avion.Tag = Vida;
            Avion.Visible = true;

            contiene.Controls.Add(Avion);
        }

        // Eventos de Teclado
        public void ActividadTecla(object? sender, KeyEventArgs e)
        {
            switch (e.KeyValue)
            {
                case 37: // Izquierda
                    if (contiene.Left < navex.Left) navex.Left -= 10;
                    break;
                case 38: // Arriba
                    if (contiene.Top < navex.Top) navex.Top -= 10;
                    break;
                case 39: // Derecha
                    if (contiene.Width > navex.Right) navex.Left += 10;
                    break;
                case 40: // Abajo
                    if (contiene.Height > navex.Bottom) navex.Top += 10;
                    break;
                case 13: // Enter (Disparar)
                    int x = navex.Location.X + (navex.Width / 2);
                    int y = navex.Location.Y;
                    CrearMisil(0, Color.DarkMagenta, "Misil", x, y);
                    break;
            }
        }

        public void Iniciar()
        {
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.Width = 400;
            this.Height = 600;
            this.Text = "JUEGO DE AVIONES BASICO";

            toolStripStatusLabel1.Text = "Vida del Rival: 50";
            toolStripStatusLabel2.Text = "Mi Nave: 20";
            this.KeyDown += new KeyEventHandler(ActividadTecla);

            contiene.Location = new Point(0, 0);
            contiene.BackColor = Color.AliceBlue;
            contiene.Size = new Size(400, 520);
            Controls.Add(contiene);

            Random r = new Random();
            int aleatx = r.Next(50, 250);
            int aleaty = r.Next(250, 330);

            CrearNave(navex, 0, 1, Color.SeaGreen, 20);
            navex.Location = new Point(aleatx, aleaty);

            CrearNave(naveRival, 180, 1, Color.DarkBlue, 50);
            naveRival.Location = new Point(100, 10);

            tiempo.Interval = 50;
            tiempo.Enabled = true;
            tiempo.Tick += new EventHandler(ImpactarTick);

            this.KeyPreview = true;
        }

        private async void RegistrarResultadoEnServidor(string resultado, int vidaJugador, int vidaRival)
        {
            if (partidaRegistrada)
                return;

            partidaRegistrada = true;

            try
            {
                var respuesta = await ClienteApi.PostAsJsonAsync("/api/partidas", new
                {
                    jugador = "Jugador",
                    resultado,
                    vidaJugador,
                    vidaRival
                });

                if (respuesta.IsSuccessStatusCode)
                {
                    toolStripStatusLabel2.Text = "Resultado guardado en el servidor";
                    return;
                }

                toolStripStatusLabel2.Text = "No se pudo guardar el resultado";
            }
            catch (HttpRequestException)
            {
                toolStripStatusLabel2.Text = "Servidor no disponible: resultado no guardado";
            }
            catch (TaskCanceledException)
            {
                toolStripStatusLabel2.Text = "Tiempo agotado: resultado no guardado";
            }
        }
    }
}