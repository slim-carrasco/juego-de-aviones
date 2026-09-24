using System.Drawing.Drawing2D;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace demo_avion
{
    public partial class Form1 : Form
    {
        //*********** VARIABLES GLOBALES ***************//
        PictureBox navex = new PictureBox();
        PictureBox naveRival = new PictureBox();
        PictureBox contiene = new PictureBox();
        System.Windows.Forms.Timer tiempo;
        int Dispara = 0;
        bool flag = false;
        float angulo = 0;

        // Controles de interfaz (StatusStrip)
        StatusStrip statusStrip1 = new StatusStrip();
        ToolStripStatusLabel toolStripStatusLabel1 = new ToolStripStatusLabel();
        ToolStripStatusLabel toolStripStatusLabel2 = new ToolStripStatusLabel();

        //*********** DIAGRAMAR DEL MISIL ***********//
        public void CrearMisil(int AngRotar, Color pintar, string nombre, int x, int y)
        {
            dynamic Balas = new PictureBox();
            int PosX = 1;
            int PosY = 1;
            int largoM = 11;
            int anchoM = 8;

            Point[] myMisil1 = {
                new Point(4*PosX,0*PosY), new Point(5*PosX,1*PosY), new Point(6*PosX,2*PosY),
                new Point(6*PosX,7*PosY), new Point(7*PosX,8*PosY), new Point(8*PosX,9*PosY),
                new Point(7*PosX,9*PosY), new Point(6*PosX,10*PosY), new Point(2*PosX,10*PosY),
                new Point(1*PosX,9*PosY), new Point(0*PosX,9*PosY), new Point(1*PosX,8*PosY),
                new Point(2*PosX,7*PosY), new Point(2*PosX,2*PosY), new Point(3*PosX,1*PosY),
                new Point(4*PosX,0*PosY)
            };
            Point[] myMisil = new Point[myMisil1.Count()];
            for (int i = 0; i < myMisil1.Count(); i++)
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
            contiene.Controls.Add(Balas);
            Balas.Visible = true;
            Balas.Tag = nombre;

            Bitmap flagImg = new Bitmap(anchoM, largoM);
            Graphics flagImagen = Graphics.FromImage(flagImg);
            flagImagen.FillRectangle(Brushes.Orange, 2, 8, 5, 1);
            flagImagen.FillRectangle(Brushes.Yellow, 3, 10, 3, 1);
            Balas.Image = flagImg;
        }

        //***************** DESTRUCTOR DEL MISIL ***********************//
        private void ImpactarTick(object sender, EventArgs e)
        {
            int X = naveRival.Location.X;
            int Y = naveRival.Location.Y;
            int W = naveRival.Width;
            int H = naveRival.Height;
            int PH = 6;
            int X2 = navex.Location.X;
            int Y2 = navex.Location.Y;
            int W2 = navex.Width;
            int H2 = navex.Height;
            int x = naveRival.Location.X;
            int y = naveRival.Location.Y;

            Dispara++;
            if (Dispara == 100 && naveRival.Visible == true)
            {
                int xRival = naveRival.Location.X + (naveRival.Width / 2);
                int yRival = naveRival.Location.Y + (naveRival.Height / 2);
                CrearMisil(180, Color.DarkRed, "Rival", xRival, yRival);
                Dispara = 0;
            }

            if (flag == false)
            {
                if (contiene.Width == x + naveRival.Width)
                    flag = true;
                x++;
            }
            else
            {
                if (contiene.Location.X == x)
                    flag = false;
                x--;
            }
            naveRival.Location = new Point(x, y);
            naveRival.BorderStyle = BorderStyle.FixedSingle;

            foreach (Control c in contiene.Controls.OfType<PictureBox>().ToList())
            {
                int X1 = c.Location.X;
                int Y1 = c.Location.Y;
                int W1 = c.Width;
                int H1 = c.Height;
                string nombre = c.Tag?.ToString() ?? "";

                if (X < X1 && X1 + W1 < X + W && Y < Y1 && Y1 + H1 < Y + H && nombre.StartsWith("Misil"))
                {
                    if (X + PH < X1 && X1 + W1 < X + W - PH)
                    {
                        c.Dispose();
                        naveRival.Tag = int.Parse(naveRival.Tag.ToString()) - 10;
                    }
                    else
                    {
                        c.Dispose();
                        naveRival.Tag = int.Parse(naveRival.Tag.ToString()) - 1;
                    }
                    toolStripStatusLabel1.Text = "Vida del Rival : " + naveRival.Tag.ToString();
                }
                else if (int.Parse(naveRival.Tag.ToString()) <= 0)
                {
                    naveRival.Dispose();
                    Bitmap NuevoImg = new Bitmap(contiene.Width, contiene.Height);
                    Graphics flagImagen = Graphics.FromImage(NuevoImg);
                    Rectangle areaTexto = new Rectangle(0, 130, contiene.Width, 60);
                    StringFormat centrado = new StringFormat { Alignment = StringAlignment.Center };
                    flagImagen.DrawString("Felicitaciones Ganaste !", new Font("Arial", 14), Brushes.Blue, areaTexto, centrado); contiene.Image = NuevoImg;
                    tiempo.Stop();
                    System.IO.File.WriteAllText("resultado.txt", "Ganaste");
                }

                if (X2 < X1 && X1 + W1 < X2 + W2 && Y2 < Y1 && Y1 + H1 < Y2 + H2 && nombre == "Rival")
                {
                    if (X2 + PH < X1 && X1 + W1 < X2 + W2 - PH)
                    {
                        c.Dispose();
                        navex.Tag = int.Parse(navex.Tag.ToString()) - 10;
                    }
                    else
                    {
                        c.Dispose();
                        navex.Tag = int.Parse(navex.Tag.ToString()) - 1;
                    }
                    toolStripStatusLabel2.Text = "Mi Nave : " + navex.Tag.ToString();
                }
                else if (int.Parse(navex.Tag.ToString()) <= 0)
                {
                    navex.Dispose();
                    Bitmap NuevoImg = new Bitmap(contiene.Width, contiene.Height);
                    Graphics flagImagen = Graphics.FromImage(NuevoImg);
                    Rectangle areaTexto = new Rectangle(0, 130, contiene.Width, 60);
                    StringFormat centrado = new StringFormat { Alignment = StringAlignment.Center };
                    flagImagen.DrawString("Felicitaciones Ganaste !", new Font("Arial", 14), Brushes.Blue, areaTexto, centrado);
                    contiene.Image = NuevoImg;
                    tiempo.Stop();
                    System.IO.File.WriteAllText("resultado.txt", "Perdiste");
                }

                if (c.Location.Y <= 0 && nombre.StartsWith("Misil")) c.Dispose();
                if (c.Location.Y >= contiene.Height && nombre == "Rival") c.Dispose();
                if (nombre == "Misil") c.Top -= 10;
                if (nombre == "Misil-Izq") { c.Top -= 10; c.Left -= 5; }
                if (nombre == "Misil-Der") { c.Top -= 10; c.Left += 5; }
                if (nombre == "Rival") c.Top += 10;

                if (W >= X2 && H >= Y2 && W2 >= X && H2 >= Y)
                {
                    naveRival.Dispose();
                    navex.Dispose();
                }
            }
            if (!tiempo.Enabled) return; // ya se detuvo el juego, no sigas

            if (int.Parse(naveRival.Tag.ToString()) <= 0)
            {
                naveRival.Dispose();
                Bitmap NuevoImg = new Bitmap(contiene.Width, contiene.Height);
                Graphics flagImagen = Graphics.FromImage(NuevoImg);
                Rectangle areaTexto = new Rectangle(0, 130, contiene.Width, 60);
                StringFormat centrado = new StringFormat { Alignment = StringAlignment.Center };
                flagImagen.DrawString("Felicitaciones Ganaste !", new Font("Arial", 14), Brushes.Blue, areaTexto, centrado);
                contiene.Image = NuevoImg;
                tiempo.Stop();
                System.IO.File.WriteAllText("resultado.txt", "Ganaste");
            }
            else if (int.Parse(navex.Tag.ToString()) <= 0)
            {
                navex.Dispose();
                Bitmap NuevoImg = new Bitmap(contiene.Width, contiene.Height);
                Graphics flagImagen = Graphics.FromImage(NuevoImg);
                Rectangle areaTexto = new Rectangle(0, 130, contiene.Width, 60);
                StringFormat centrado = new StringFormat { Alignment = StringAlignment.Center };
                flagImagen.DrawString("Perdiste el Juego", new Font("Arial", 14), Brushes.Red, areaTexto, centrado);
                contiene.Image = NuevoImg;
                tiempo.Stop();
                System.IO.File.WriteAllText("resultado.txt", "Perdiste");
            }
        }

        //*********** DIAGRAMAR NAVE ***********//
        public void CrearNave(PictureBox Avion, int AngRotar, int Tipox, Color Pintar, int Vida)
        {
            int largoN = 0;
            int anchoN = 0;

            Point[] myNave1 = {
                new Point(29,0), new Point(30,1), new Point(30,6), new Point(31,6),
                new Point(31,11), new Point(32,11), new Point(32,17), new Point(35,17), new Point(35,16),
                new Point(37,16), new Point(37,17), new Point(38,18), new Point(38,28), new Point(39,28),
                new Point(42,39), new Point(44,45), new Point(50,51), new Point(51,51), new Point(51,52),
                new Point(58,59), new Point(58,66), new Point(44,66), new Point(39,71), new Point(35,71),
                new Point(35,74), new Point(32,77), new Point(26,77), new Point(23,74), new Point(23,71),
                new Point(19,71), new Point(14,66), new Point(0,66), new Point(0,59), new Point(7,52), new Point(7,51),
                new Point(8,51), new Point(14,45), new Point(16,39), new Point(19,28), new Point(20,28),
                new Point(20,18), new Point(26,11), new Point(27,11), new Point(27,6), new Point(28,6),
                new Point(28,1), new Point(29,0)
            };
            Point[] myNave2 = {
                new Point(16, 0), new Point(17, 1), new Point(18, 2), new Point(18, 3),
                new Point(19, 3), new Point(20, 3), new Point(21, 4), new Point(22, 5), new Point(23, 6),
                new Point(24, 7), new Point(25, 8), new Point(26, 9), new Point(25, 9), new Point(24, 9),
                new Point(23, 9), new Point(22, 9), new Point(21, 10), new Point(20, 11), new Point(20, 12), new Point(20, 13),
                new Point(21, 14), new Point(22, 15), new Point(22, 16), new Point(22, 17), new Point(22, 18),
                new Point(22, 19), new Point(22, 20), new Point(22, 21), new Point(22, 22), new Point(23, 23),
                new Point(24, 24), new Point(25, 25), new Point(26, 26), new Point(27, 27), new Point(28, 28),
                new Point(29, 29), new Point(30, 30), new Point(31, 31), new Point(32, 32), new Point(33, 33),
                new Point(33, 34), new Point(33, 35), new Point(33, 36), new Point(33, 37), new Point(33, 38),
                new Point(33, 39), new Point(33, 40), new Point(32, 40), new Point(31, 40), new Point(30, 40),
                new Point(29, 41), new Point(28, 42), new Point(27, 42), new Point(26, 42), new Point(25, 41),
                new Point(24, 40), new Point(23, 40), new Point(22, 40), new Point(21, 40), new Point(20, 40),
                new Point(19, 40), new Point(18, 40), new Point(17, 41), new Point(16, 42), new Point(15, 41),
                new Point(14, 40), new Point(13, 40), new Point(12, 40), new Point(11, 40), new Point(10, 40),
                new Point(9, 40), new Point(8, 41), new Point(7, 42), new Point(6, 42), new Point(5, 42), new Point(4, 41),
                new Point(3, 40), new Point(2, 40), new Point(1, 40), new Point(0, 40), new Point(0, 39),
                new Point(0, 38), new Point(0, 37), new Point(0, 36), new Point(0, 35), new Point(0, 34), new Point(0, 33),
                new Point(1, 32), new Point(2, 31), new Point(3, 30), new Point(4, 29), new Point(5, 28),
                new Point(6, 27), new Point(7, 26), new Point(8, 25), new Point(9, 24), new Point(10, 23),
                new Point(11, 22), new Point(11, 21), new Point(11, 20), new Point(11, 19), new Point(11, 18),
                new Point(11, 17), new Point(11, 16), new Point(11, 15), new Point(11, 14), new Point(12, 13),
                new Point(12, 12), new Point(12, 11), new Point(11, 10), new Point(10, 9), new Point(9, 9),
                new Point(8, 9), new Point(7, 9), new Point(6, 9), new Point(7, 8), new Point(8, 7), new Point(9, 6),
                new Point(10, 5), new Point(11, 4), new Point(12, 3), new Point(13, 3), new Point(14, 3),
                new Point(14,2), new Point(15, 1), new Point(16, 0)
            };
            Point[] myNave3 = {
                new Point(25, 54), new Point(26, 54), new Point(26, 50),
                new Point(26, 49), new Point(27, 50), new Point(28, 50), new Point(29, 50), new Point(30, 51),
                new Point(31, 51), new Point(32, 52), new Point(32, 49), new Point(31, 48), new Point(30, 47),
                new Point(29, 46), new Point(28, 45), new Point(27, 44), new Point(27,36), new Point(28, 35),
                new Point(28, 25), new Point(29, 25), new Point(30, 25), new Point(31, 25), new Point(32, 26),
                new Point(33,26), new Point(34, 27), new Point(35, 28), new Point(36, 28), new Point(37, 29),
                new Point(38, 30), new Point(39,30), new Point(40,31), new Point(41,32), new Point(42,32),
                new Point(43, 33), new Point(44, 34), new Point(45, 35), new Point(46, 36), new Point(47, 36),
                new Point(48, 36), new Point(49,37), new Point(50,37), new Point(51,38), new Point(51,37),
                new Point(51,36), new Point(51,35), new Point(50,35), new Point(37,22), new Point(37,15),
                new Point(36,14), new Point(35,14), new Point(34,15), new Point(34,21), new Point(28,15),
                new Point(28,7), new Point(27,6), new Point(26,5), new Point(25,5), new Point(24,6), new Point(23, 7),
                new Point(23,15), new Point(17, 21), new Point(17,15), new Point(16,14), new Point(15,14),
                new Point(14,15), new Point(14,22), new Point(1, 35), new Point(0, 35), new Point(0,36),
                new Point(0,37), new Point(0,38), new Point(1,37), new Point(2,37), new Point(3,36), new Point(4,36),
                new Point(5,36), new Point(6,35), new Point(7,34), new Point(8,33), new Point(9,32),
                new Point(10,32), new Point(11,31), new Point(12, 30), new Point(13,30), new Point(14,29),
                new Point(15,28), new Point(16,28), new Point(17,27), new Point(18,26), new Point(19,26),
                new Point(15,28), new Point(16, 28), new Point(17, 27), new Point(18, 26), new Point(19,26),
                new Point(20,25), new Point(21,25), new Point(22,25), new Point(23,25), new Point(23,35),
                new Point(24,36), new Point(24,44), new Point(23,45), new Point(22, 46), new Point(21,47),
                new Point(20,48), new Point(19,49), new Point(19,52), new Point(20, 51), new Point(21,51),
                new Point(22,50), new Point(23,50), new Point(24,50), new Point(25,50), new Point(25,54)
            };

            Point[] myNave;
            GraphicsPath ObjGrafico = new GraphicsPath();
            if (Tipox == 1)
            {
                largoN = 77;
                anchoN = 58;
                myNave = new Point[myNave1.Count()];
                for (int i = 0; i < myNave1.Count(); i++)
                {
                    myNave[i].X = myNave1[i].X;
                    if (AngRotar == 180)
                        myNave[i].Y = largoN - myNave1[i].Y;
                    else
                        myNave[i].Y = myNave1[i].Y;
                }
                ObjGrafico.AddPolygon(myNave);
            }
            else if (Tipox == 2)
            {
                largoN = 42;
                anchoN = 33;
                myNave = new Point[myNave2.Count()];
                for (int i = 0; i < myNave2.Count(); i++)
                {
                    myNave[i].X = myNave2[i].X;
                    if (AngRotar == 180)
                        myNave[i].Y = largoN - myNave2[i].Y;
                    else
                        myNave[i].Y = myNave2[i].Y;
                }
                ObjGrafico.AddPolygon(myNave);
            }
            else if (Tipox == 3)
            {
                largoN = 54;
                anchoN = 51;
                myNave = new Point[myNave3.Count()];
                for (int i = 0; i < myNave3.Count(); i++)
                {
                    myNave[i].X = myNave3[i].X;
                    if (AngRotar == 180)
                        myNave[i].Y = largoN - myNave3[i].Y;
                    else
                        myNave[i].Y = myNave3[i].Y;
                }
                ObjGrafico.AddPolygon(myNave);
            }

            Avion.BackColor = Pintar;
            Avion.Size = new Size(anchoN, largoN);
            Avion.Region = new Region(ObjGrafico);
            Avion.Location = new Point(0, 0);
            contiene.Controls.Add(Avion);

            NaveCorre(Avion, AngRotar, 0);
            Avion.Tag = Vida;
            Avion.Visible = true;
        }

        //*********** EFECTOS DE LA NAVE PRINCIPAL ***********//
        public void NaveCorre(PictureBox Avion, int AngRotar, int velox)
        {
            Bitmap Imagen = new Bitmap(Avion.Width, Avion.Height);
            Graphics PintaImg = Graphics.FromImage(Imagen);

            Point[] puntoDer = {
                new Point(47, 69), new Point(47, 76),
                new Point(51, 76), new Point(51, 69)
            };

            PintaImg.FillPolygon(Brushes.OrangeRed, puntoDer);
            Avion.Image = Imagen;
        }

        //*********** MOVIMIENTO DEL TECLADO DEL USUARIO ***********//
        public void ActividadTecla(object sender, KeyEventArgs e)
        {
            switch (e.KeyValue)
            {
                case 37: // flecha izquierda
                    if (contiene.Left < navex.Left)
                        navex.Left -= 10;
                    angulo = -15;
                    NaveCorre(navex, 1, 0);
                    break;
                case 38: // flecha arriba
                    if (contiene.Top < navex.Top)
                        navex.Top -= 10;
                    NaveCorre(navex, 0, 1);
                    break;
                case 39: // flecha derecha
                    if (contiene.Right > navex.Right)
                        navex.Left += 10;
                    angulo = +15;
                    NaveCorre(navex, 1, 0);
                    break;
                case 40: // flecha abajo
                    if (contiene.Bottom > navex.Bottom)
                        navex.Top += 10;
                    NaveCorre(navex, 0, 1);
                    break;
                case 13: // Enter (disparar)
                    tiempo.Start();
                    int x = navex.Location.X + (navex.Width / 2);
                    int y = navex.Location.Y + (navex.Height / 2);
                    CrearMisil(0, Color.DarkMagenta, "Misil", x, y);
                    break;
                case 32: // barra espaciadora (escopeta)
                    tiempo.Start();
                    int xE = navex.Location.X + (navex.Width / 2);
                    int yE = navex.Location.Y + (navex.Height / 2);
                    CrearMisil(0, Color.DarkMagenta, "Misil-Izq", xE, yE);
                    CrearMisil(0, Color.DarkMagenta, "Misil", xE, yE);
                    CrearMisil(0, Color.DarkMagenta, "Misil-Der", xE, yE);
                    break;
            }
        }

        //*********** ACTIVAR ACCIONES DE INICIALIZACION ***********//
        public void Iniciar()
        {
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.Width = 400;
            this.Height = 600;
            this.Text = "JUEGO DE AVIONES BASICO";

            statusStrip1.Items.Add(toolStripStatusLabel1);
            statusStrip1.Items.Add(toolStripStatusLabel2);
            this.Controls.Add(statusStrip1);
            toolStripStatusLabel1.Text = "Mi Rival";
            toolStripStatusLabel2.Text = "Mi Avion";

            this.KeyDown += new KeyEventHandler(ActividadTecla);

            contiene.Location = new Point(0, 0);
            contiene.BackColor = Color.AliceBlue;
            contiene.Size = new Size(400, 570);
            Controls.Add(contiene);
            contiene.Visible = true;

            Random r = new Random();
            int aleatY = r.Next(250, 330);
            int aleatX = r.Next(50, 250);
            CrearNave(navex, 0, 1, Color.SeaGreen, 20);

            Random sal = new Random();
            int sale = sal.Next(1, 3);
            CrearNave(naveRival, 180, 1, Color.DarkBlue, 50);
            navex.Location = new Point(aleatX, aleatY);

            tiempo = new System.Windows.Forms.Timer();
            tiempo.Interval = 50;
            tiempo.Enabled = true;
            tiempo.Tick += new EventHandler(ImpactarTick);

            this.KeyPreview = true;
        }

        public Form1()
        {
            InitializeComponent();
            Iniciar();
        }
    }
}
