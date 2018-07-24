using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kosynka
{
    public partial class Form1 : Form
    {
        public const int wCard = 100, hCard = 140;                  // размер карты
        public const int wOffset = wCard / 4, hOffset = hCard / 8;  // расстояния между картами
        public const int wShift = wCard / 5, hShift = hCard / 5; 
        public const int wField = (wCard + wOffset) * 10 + wOffset, hField = (int)((hCard + hOffset) * 5.5), wWindow = wField + 17, offset = 25, hWindow = hField + 40 + offset;  // учитываем края

        public int time = 0;
        public int variant = 0;
        bool win = false;
        int accent = -1, accent2 = -1, accentLen = 1;

        int oldex, oldey, x, y;    // координаты перемещаемой фигуры
        int candOldPlace, oldPlace, newPlace;    // каждое место имеет свой код от 0 до 11
        bool dragging = false;
        bool animation = true;     // красивое перемещение карт, плавное
        bool animation2 = false;   // между собой там идет определение

        List<Card>[] stacks;       // ну угадайте что это
        List<Card> rest;

        List<Card> buffer;         // для переноса

        Random rnd = new Random();
        int[] used;                // нужно при растасовке карт
        int countRemember;         // кол-во переложенных карт на последнем ходе (вместо памятного буфера)
        bool needToClose = false;  // скрыть карту при отмене хода
        int num = 0;               // номер стека, для которого проводится анимация  
        int counter = 0;           // счетчик (для таймера4)
        Card naglCard;             // карта которая будет пока отображаться вместо короля (для таймера4)

        int quanOfPiles = 0;       // кол-во собранных стопок
        int quanOfAdds = 6;        // кол-во стопок на раскладку
        float h = 50;              // цветовой параметр
        Color winColor;
        bool returnWas = false;    // как было в отмене хода

        public Form1()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(0, 140, 70);
            winColor = HsvToRgb(h, 1f, 1f);
            this.Width = wWindow;
            this.Height = hWindow;

            stacks = new List<Card>[10];

            for (int i = 0; i < 10; ++i)
            {
                stacks[i] = new List<Card>();
                stacks[i].Clear();
            }

            rest = new List<Card>();
            buffer = new List<Card>();

            used = new int[104];
            for (int i = 0; i < 104; ++i)
            {
                used[i] = i % 52;
            }

            Shuffle();
            FillStacksAndList();
        }

        public void swap(ref int a, ref int b)
        {
            int tmp = a;
            a = b;
            b = tmp;
        }

        private void Shuffle(){
            for (int i = 0; i < 104; ++i)
            {
                swap(ref used[i], ref used[rnd.Next(0, 104)]);
            }
        }

        private void FillStacksAndList()
        {
            int index = 0;

            for (int i = 0; i < 10; ++i)
            {
                for (int j = 0; j < 5 - (i + 6) / 10; ++j){
                    /*bool visible = j == 4 - (i + 6) / 10;*/
                    stacks[i].Add(new Card(2, used[index] % 13 + 1, false/*visible*/));
                    ++index;
                }
            }

            while (index < 104)
            {
                rest.Add(new Card(2, used[index] % 13 + 1, true));
                ++index;
            }

            AddCardsToStacksFromRest();
            отменитьХодToolStripMenuItem.Enabled = false;
        }

        private String GetNameOfPic(Card card)
        {
            String s = ""; 
            if (card.opened)
            {
                switch (card.suit)
                {
                    case 0: s += "clubs"; break;
                    case 1: s += "hearts"; break;
                    case 2: s += "spades"; break;
                    case 3: s += "diamonds"; break;
                }
                s += card.number;
            }
            else
            {
                s = "shirt" + Data.numShirt;
            }

            return s;
        }

        public static bool IsIn(int x, int y, int start, int start2, int len, int len2)   // находится в прямоугольнике (с учетом offset)
        {
            return x >= start && x <= start + len && y >= start2 && y <= start2 + len2;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.White);
            Pen yellow_pen = new Pen(Color.Yellow, 5);
            Brush win_brush = new SolidBrush(winColor);
            Font font = new Font("Arial", 48, FontStyle.Bold);
            Image image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject("shirt" + Data.numShirt);

            // отображаем карты на раскладку
            for (int i = 0; i < quanOfAdds; i++)
                g.DrawImage(image, wField - wOffset - wCard - i * wShift, hField - hOffset - hCard, wCard, hCard);

            // отображаем "готовых" королей
            image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject("spades13");
            for (int i = 0; i < quanOfPiles; i++)
            {
                g.DrawImage(image, wOffset + i * wShift, hField - hOffset - hCard, wCard, hCard);
            }

            // отображаем наглую карту
            if (naglCard != null)
            {
                String s = GetNameOfPic(naglCard);
                image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject(s);
                g.DrawImage(image, wOffset + quanOfPiles * wShift, hField - hOffset - hCard, wCard, hCard);
            }
                
            // отображаем стеки
            for (int i = 0; i < 10; i++)
            {
                if (stacks[i].Count == 0)
                {
                    g.DrawRectangle(pen, wOffset + (wOffset + wCard) * i, offset + hOffset, wCard, hCard);
                }
                else
                {
                    for (int j = 0; j < stacks[i].Count; j++)
			        {
                        String s = GetNameOfPic(stacks[i][j]);
                        image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject(s);
                        g.DrawImage(image, wOffset + (wOffset + wCard) * i, offset + hOffset + hShift * j, wCard, hCard);
			        }
                }
            }

            if (dragging || animation)
            {
                String s;

                for (int i = 0; i < buffer.Count; i++)
                {
                    if (buffer[i] != null)
                    {
                        s = GetNameOfPic(buffer[i]);
                        image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject(s);
                        g.DrawImage(image, x, y + hShift * i, wCard, hCard);
                    }
                }
            }

            // для случая подсказки
            if (accent > -1)
            {
                if (accent < 10)
                    g.DrawRectangle(yellow_pen, wOffset + (wOffset + wCard) * accent, offset + hOffset + hShift * ((stacks[accent].Count > 0 ? stacks[accent].Count : stacks[accent].Count + 1) - 1) - (accentLen - 1) * hShift, wCard, hCard + (accentLen - 1) * hShift);
                else
                    g.DrawRectangle(yellow_pen, wField - wOffset - wCard - (quanOfAdds - 1) * wShift, hField - hOffset - hCard, wCard, hCard);
            }

            if (accent2 > -1)
            {
                if (accent2 < 10)
                    g.DrawRectangle(yellow_pen, wOffset + (wOffset + wCard) * accent2, offset + hOffset + hShift * ((stacks[accent2].Count > 0 ? stacks[accent2].Count : stacks[accent2].Count + 1) - 1), wCard, hCard);
                else
                    g.DrawRectangle(yellow_pen, wField - wOffset - wCard - (quanOfAdds - 1) * wShift, hField - hOffset - hCard, wCard, hCard);
            }

            // для случая победы
            if (win)
            {
                g.DrawString("Вы выиграли!", font, win_brush, wField / 2 - 235, hField / 2 - 30);
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ++time;
            toolStripStatusLabel1.Text = "Время: " + time;
        }

        public class Card
        {
            public int suit;   // 1 - clubs, 2 - hearts, 3 - spades, 4 - diamonds
            public int number; // 11 - валет, 12 - дама, 13 - король
            public bool opened;

            public Card(int suit, int number, bool opened = false)
            {
                this.suit = suit;
                this.number = number;
                this.opened = opened;
            }
        }

        bool isNull(Card card)
        {
            return card == null;
        }

        void ClearNulls(List<Card> list)
        {
            list.RemoveAll(isNull);
        }

        bool TryStroke(MouseEventArgs e)  // пытаемся сделать ход 
        {
            // если координаты сейчас на прямоугольнике, проверяем, можно ли так, и если да, то переносим с буфера туда
            // проверяем стеки
            for (int i = 0; i < 10; i++)
            {
                int j = stacks[i].Count - 1;

                if (IsIn(e.X, e.Y, wOffset + (wOffset + wCard) * i, offset + hOffset + hShift * j, wCard, hCard))
                {
                    if (stacks[i].Count == 0 || condNorm(stacks[i][stacks[i].Count-1], buffer[0])){
                        countRemember = buffer.Count;   // для отмены хода
                        oldPlace = candOldPlace;
                        newPlace = i;
                        ToNewPlace();

                        // проверить открылась ли карта
                        needToClose = oldPlace < 10 && stacks[oldPlace].Count > 0 && stacks[oldPlace][stacks[oldPlace].Count-1].opened == false;

                        отменитьХодToolStripMenuItem.Enabled = true;

                        return true;
                    }
                    else{
                        return false;
                    }
                }
                
            }

            return false;
        }

        void Put(int place)
        {
            for (int i = 0; i < buffer.Count; i++)
            {
                stacks[place].Add(buffer[i]);
            }
        }

        void GetBack()   // возврат из буфера на место
        {
            Put(oldPlace);
        }

        void ToNewPlace()  // кладем на новое место
        {
            Put(newPlace);
        }

        void AddCardsToStacksFromRest(){
            /*for (int i = 0; i < 10; i++)
            {
                stacks[i].Add(rest[0]);
                rest.RemoveAt(0);
            }*/
            animation = true;
            num = 0;
            timer3.Start();
            //новаяИграToolStripMenuItem.Enabled = false;
            returnWas = отменитьХодToolStripMenuItem.Enabled;
            отменитьХодToolStripMenuItem.Enabled = false;
            подсказкаToolStripMenuItem.Enabled = false;
        }

        void ReturnInAdd()
        {
            for (int i = 9; i >= 0; i--)
            {
                rest.Insert(0, stacks[i][stacks[i].Count - 1]);
                stacks[i].RemoveAt(stacks[i].Count - 1);
            }
        }

        void openStacksIfPossible()
        {
            for (int i = 0; i < 10; i++)
            {
                if (stacks[i].Count > 0)
                {
                    stacks[i][stacks[i].Count - 1].opened = true;
                }
            }
        }

        void canWeTakeIt()   // если можем забрать стопку, то забираем
        {
            for (int i = 0; i < 10; i++)
            {
                if (stacks[i].Count >= 13 && stacks[i][stacks[i].Count - 13].opened)
                {
                    // формируем из них буфер и проверяем на "нормальность" =)
                    for (int j = 0; j < 13; j++)
                    {
                        buffer.Add(stacks[i][stacks[i].Count - 13 + j]);
                    }

                    if (normBuffer())
                    {
                        animation = true;
                        num = i;
                        counter = 0;
                        timer4.Start();
                        отменитьХодToolStripMenuItem.Enabled = false;
                        подсказкаToolStripMenuItem.Enabled = false;

                        // в таймере надо карты по одной направлять к выходу

                        // забираем карты
                        //stacks[i].RemoveRange(stacks[i].Count - 13, 13);
                    }

                    buffer.Clear();
                }
            }
        }

        bool IsThereOneEmptyStack()
        {
            for (int i = 0; i < 10; i++)
            {
                if (stacks[i].Count == 0)
                    return true;
            }
            return false;
        }

        bool condNorm(Card c1, Card c2)   // условие для буфера, чтоб можно было карты друг на друга в стеке класть
        {
            return c1.number - c2.number == 1;
        }

        bool normBuffer()
        {
            for (int i = 0; i < buffer.Count - 1; i++)
            {
                if (!condNorm(buffer[i], buffer[i + 1]))
                {
                    return false;
                }
            }
            return true;
        }

        bool normStack(int number)
        {
            int pos = 0;
            while (!stacks[number][pos].opened)
                ++pos;

            for (int i = pos; i < stacks[number].Count - 1; i++)
            {
                if (!condNorm(stacks[number][i], stacks[number][i + 1]))
                {
                    return false;
                }
            }

            return true;
        }

        int normStackNumber(int number)  // всегда хотя бы один элемент в стеке будет норм
        {
            bool uslNorm = true;
            int pos = 0;
            while (!stacks[number][pos].opened)
                ++pos;

            for (int i = pos; i < stacks[number].Count - 1; i++)
            {
                uslNorm = true;

                for (int j = i; j < stacks[number].Count - 1; j++)
                {
                    if (!condNorm(stacks[number][j], stacks[number][j + 1]))
                    {
                        uslNorm = false;
                    }
                }

                if (uslNorm)
                {
                    return i;
                }
            }

            return stacks[number].Count - 1;
        }

        bool Win()    // критерий победы
        {
            return quanOfPiles == 8;
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!win && !animation)
            {
                accent = -1;
                accent2 = -1;

                oldex = e.X;
                oldey = e.Y;

                // новая стопка раскладывается
                if (quanOfAdds > 0 && IsIn(e.X, e.Y, wField - wOffset - wCard - quanOfAdds * wShift, hField - hOffset - hCard, wCard, hCard))
                {
                    if (IsThereOneEmptyStack())
                    {
                        MessageBox.Show("В каждой стопке должна быть хотя бы одна карта","Сообщение");
                    }
                    else
                    {
                        oldPlace = 10;
                        newPlace = 10;
                        отменитьХодToolStripMenuItem.Enabled = true;
                        AddCardsToStacksFromRest();
                        Invalidate();
                    }
                }

                // проверяем стеки
                for (int i = 0; i < 10; i++)
                {
                    for (int j = stacks[i].Count - 1; j >= 0; j--)
                    {
                        if (IsIn(e.X, e.Y, wOffset + (wOffset + wCard) * i, offset + hOffset + hShift * j, wCard, hCard) && stacks[i][j].opened)
                        {
                            int k = j;
                            while (k < stacks[i].Count)
                            {
                                buffer.Add(stacks[i][k]);
                                stacks[i].RemoveAt(k);
                            }

                            candOldPlace = i;
                            dragging = normBuffer();
                            if (!normBuffer())
                            {
                                /*GetBack();*/
                                Put(candOldPlace);
                                buffer.Clear();    // может это поможет??? Помогло кажись
                                return;
                            }
                            x = wOffset + (wOffset + wCard) * i;
                            y = offset + hOffset + hShift * j;

                            return;/*break;*/
                        }
                    }
                }
            }        
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (dragging && !animation)
            {
                if (!TryStroke(e))
                    Put(candOldPlace);
                    /*GetBack();*/
                dragging = false;
                buffer.Clear();
                canWeTakeIt();
                openStacksIfPossible();
            }

            Invalidate();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                x += e.X - oldex;
                y += e.Y - oldey;
                Invalidate();
                oldex = e.X;
                oldey = e.Y;
            }
        }

        private void новаяИграToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timer2.Stop();

            time = 0;
            variant = 0;
            win = false;
            accent = -1;
            accent2 = -1;
            quanOfPiles = 0;    
            quanOfAdds = 6;
            h = 50;
            winColor = HsvToRgb(h, 1f, 1f);
            dragging = false;
            animation = false;
            animation2 = false;

            for (int i = 0; i < 10; ++i)
            {
                stacks[i].Clear();
            }

            rest.Clear();
            buffer.Clear();

            for (int i = 0; i < 104; ++i)
            {
                used[i] = i % 52;
            }

            Shuffle();
            FillStacksAndList();

            //подсказкаToolStripMenuItem.Enabled = true;
            
            timer1.Start();
            отменитьХодToolStripMenuItem.Enabled = false;

            Invalidate();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void сменитьРубашкуToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 Form2 = new Form2();
            Form2.ShowDialog();
            Invalidate();
        }

        private void справкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Пасьянс \"Паук\"\nWindows XP\n\nПрограммист: Гумеров Артур", "Справка");
        }

        static Color HsvToRgb(float h, float s, float v)
        {
            int i;
            float f, p, q, t;

            if (s < float.Epsilon)
            {
                int c = (int)(v * 255);
                return Color.FromArgb(c, c, c);
            }

            h /= 60;
            i = (int)Math.Floor(h);
            f = h - i;
            p = v * (1 - s);
            q = v * (1 - s * f);
            t = v * (1 - s * (1 - f));

            float r, g, b;
            switch (i)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }

            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private void timer2_Tick(object sender, EventArgs e)   // карты падают волнами
        {
            h += 2;
            if (h >= 360) h = 0;
            winColor = HsvToRgb(h, 1f, 1f);
            Invalidate();
        }

        private void отменитьХодToolStripMenuItem_Click(object sender, EventArgs e)
        {
            отменитьХодToolStripMenuItem.Enabled = false;

            // если просто нажали на магазин
            if (oldPlace == 10)
            {
                ++quanOfAdds;
                ReturnInAdd();
            }
            else
            {
                // скрыть открытую карту из стека
                if (needToClose)
                {
                    stacks[oldPlace][stacks[oldPlace].Count - 1].opened = false;
                }

                // переложить countRemember карт с newPlace на oldPlace

                // I. переложить countRemember карт с newPlace на buffer

                for (int i = 0; i < countRemember; i++)
                {
                    // сначала удаляем из нового места
                    Card card;
                    
                    card = stacks[newPlace][stacks[newPlace].Count - 1];
                    stacks[newPlace].RemoveAt(stacks[newPlace].Count - 1);
                    
                    // затем в буфер
                    buffer.Add(card);
                }

                // II. переложить countRemember карт с buffer на oldPlace

                for (int i = 0; i < countRemember; i++)
                {
                    // сначала удаляем из буфера
                    Card card = buffer[buffer.Count - 1];
                    buffer.RemoveAt(buffer.Count - 1);

                    // затем возвращаем в старое
                    stacks[oldPlace].Add(card);
                }
            }

            Invalidate();
        }

        private void подсказкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            accentLen = 1;

            // смотрим где что подходит
            // потом со стека на стек хотим перекладывать (с j на i)
            for (int j = 9; j >= 0; j--)
                if (stacks[j].Count > 0)
                    for (int i = 9; i >= 0; i--)
                    {
                        int k = 0;
                        while (k < stacks[j].Count && !stacks[j][k].opened) ++k;

                        if (i != j && stacks[j].Count > 0 && (stacks[i].Count == 0 || stacks[i].Count > 0 && condNorm(stacks[i][stacks[i].Count - 1], stacks[j][k])) && normStack(j))
                        {
                            accentLen = stacks[j].Count - k;
                            if (stacks[i].Count == 0 && k == 0) continue;
                            accent = j;
                            accent2 = i;
                            Invalidate();
                            return;
                        }
                    }

            // идеально не получилось, давайте попроще
            for (int j = 9; j >= 0; j--)
                if (stacks[j].Count > 0)
                    for (int i = 9; i >= 0; i--)
                    {
                        int k = normStackNumber(j);
                        //while (k < stacks[j].Count && !stacks[j][k].opened) ++k;

                        if (i != j && stacks[j].Count > 0 && (stacks[i].Count == 0 || stacks[i].Count > 0 && condNorm(stacks[i][stacks[i].Count - 1], stacks[j][k])))
                        {
                            accentLen = stacks[j].Count - k;
                            if (stacks[i].Count == 0 && k == 0) continue;
                            accent = j;
                            accent2 = i;
                            Invalidate();
                            return;
                        }
                    }

            if (rest.Count > 0)
                accent = 10;
            else MessageBox.Show("Ходов нет", "Сообщение");
            accent2 = -1;
            Invalidate();
        }

        int LinearFunction(int y, int x1, int y1, int x2, int y2)
        {
            return x1 + (y - y1) * (x1 - x2) / (y1 - y2);
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            if (num < 10)
            {
                if (!animation2)
                {
                    buffer.Add(rest[0]);
                    x = wField - wOffset - wCard - (quanOfAdds - 1) * wShift;
                    y = hField - hOffset - hCard;         
                    animation2 = true;
                    if (num == 9)
                        --quanOfAdds;
                }
                else
                {
                    y -= 100;
                    x = LinearFunction(y, wField - wOffset - wCard - (quanOfAdds - 1) * wShift, hField - hOffset - hCard, wOffset + (wOffset + wCard) * num, offset + hOffset + hShift * stacks[num].Count);
                    if (y <= offset + hOffset + hShift * stacks[num].Count){
                        stacks[num].Add(rest[0]);
                        rest.RemoveAt(0);
                        buffer.Clear();
                        animation2 = false;
                        ++num;
                    }
                }
            }
            else
            {
                timer3.Stop();
                animation = false;
                animation2 = false;
                новаяИграToolStripMenuItem.Enabled = true;
                отменитьХодToolStripMenuItem.Enabled = returnWas;
                подсказкаToolStripMenuItem.Enabled = true;
            }

            Invalidate();
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            if (counter < 13){
                //MessageBox.Show("таймер4");
                if (!animation2)
                {
                    buffer.Add(stacks[num][stacks[num].Count - 1]);
                    stacks[num].RemoveAt(stacks[num].Count - 1);     // забираем карты
                    x = wOffset + (wOffset + wCard) * num;
                    y = offset + hOffset + hShift * stacks[num].Count;         
                    animation2 = true;
                }
                else
                {
                    y += 100;
                    x = LinearFunction(y, wOffset + quanOfPiles * wShift, hField - hOffset - hCard, wOffset + (wOffset + wCard) * num, offset + hOffset + hShift * stacks[num].Count);
                    if (y >= hField - hOffset - hCard){
                        naglCard = buffer[0];
                        buffer.Clear();
                        animation2 = false;
                        ++counter;
                    }
                }
            }
            else
            {
                timer4.Stop();
                animation = false;
                animation2 = false;
                новаяИграToolStripMenuItem.Enabled = true;
                подсказкаToolStripMenuItem.Enabled = true;
                ++quanOfPiles;
                naglCard = null;
                openStacksIfPossible();

                if (!win && Win())
                {
                    timer2.Start();
                    win = true;
                    timer1.Stop();
                    отменитьХодToolStripMenuItem.Enabled = false;
                    подсказкаToolStripMenuItem.Enabled = false;
                }
            }

            Invalidate();
        }
    }

}
