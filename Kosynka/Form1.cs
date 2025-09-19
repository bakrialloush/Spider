using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Kosynka
{
    public partial class Form1 : Form
    {
        public const int wCard = 100, hCard = 140;                  // card size
        public const int wOffset = wCard / 4, hOffset = hCard / 8;  // distance between cards
        public const int wShift = wCard / 5, hShift = hCard / 5;
        // calculating edges
        public const int wField = (wCard + wOffset) * 10 + wOffset;
        public const int hField = (int)((hCard + hOffset) * 5.5);
        public const int wWindow = wField + 17;
        public const int offset = 25;
        public const int hWindow = hField + 40 + offset;

        public int time = 0;
        public int variant = 0;
        bool win = false;
        int accent = -1, accent2 = -1, accentLen = 1;

        int oldex, oldey, x, y;    // coordinates of the moving figure
        int candOldPlace;          // each place has its own code from 0 to 11
        bool dragging = false;
        bool animation = true;     // smooth card movement
        bool animation2 = false;   // internal determination

        private readonly List<int> oldPlace, newPlace, countRemember;  // number of moved cards (instead of a memory buffer)
        private readonly List<bool> needToClose;  // hide card when undoing move
        private readonly List<Card>[] stacks;       // guess what this is
        private readonly List<Card> rest;

        private readonly List<Card> buffer;         // for transfer

        private readonly Random rnd = new Random();
        private readonly int[] used;                // needed for card shuffling
        int num = 0;               // stack number for animation  
        int counter = 0;           // counter (for timer4)
        Card naglCard;             // card temporarily displayed instead of king (for timer4)

        int quanOfPiles = 0;       // number of completed piles
        int quanOfAdds = 6;        // number of piles to deal
        float h = 50;              // color parameter
        Color winColor;
        bool returnWas = false;    // as in undo move

        public Form1()
        {
            InitializeComponent();
            BackColor = Color.FromArgb(0, 140, 70);
            winColor = HsvToRgb(h, 1f, 1f);
            Width = wWindow;
            Height = hWindow;

            stacks = new List<Card>[10];

            for (int i = 0; i < 10; ++i)
            {
                stacks[i] = new List<Card>();
                stacks[i].Clear();
            }

            rest = new List<Card>();
            buffer = new List<Card>();

            oldPlace = new List<int>();
            newPlace = new List<int>();
            countRemember = new List<int>();
            needToClose = new List<bool>();

            used = new int[104];
            for (int i = 0; i < 104; ++i)
            {
                used[i] = i % 52;
            }

            Shuffle();
            FillStacksAndList();
        }

        public void Swap(ref int a, ref int b)
        {
            (b, a) = (a, b);
        }

        private void Shuffle()
        {
            for (int i = 0; i < 104; ++i)
            {
                Swap(ref used[i], ref used[rnd.Next(0, 104)]);
            }
        }

        private void FillStacksAndList()
        {
            int index = 0;

            for (int i = 0; i < 10; ++i)
            {
                for (int j = 0; j < 5 - (i + 6) / 10; ++j)
                {
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
            undoMoveToolStripMenuItem.Enabled = false;
        }

        private string GetNameOfPic(Card card)
        {
            string s = "";
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

        public static bool IsIn(int x, int y, int start, int start2, int len, int len2)   // is inside rectangle (with offset)
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
            Image image = (Image)Properties.Resources.ResourceManager.GetObject("shirt" + Data.numShirt);

            // display cards to deal
            for (int i = 0; i < quanOfAdds; i++)
                g.DrawImage(image, wField - wOffset - wCard - i * wShift, hField - hOffset - hCard, wCard, hCard);

            // display "ready" kings
            image = (Image)Properties.Resources.ResourceManager.GetObject("spades13");
            for (int i = 0; i < quanOfPiles; i++)
            {
                g.DrawImage(image, wOffset + i * wShift, hField - hOffset - hCard, wCard, hCard);
            }

            // display temporary card
            if (naglCard != null)
            {
                string s = GetNameOfPic(naglCard);
                image = (Image)Properties.Resources.ResourceManager.GetObject(s);
                g.DrawImage(image, wOffset + quanOfPiles * wShift, hField - hOffset - hCard, wCard, hCard);
            }

            // display stacks
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
                        string s = GetNameOfPic(stacks[i][j]);
                        image = (Image)Properties.Resources.ResourceManager.GetObject(s);
                        g.DrawImage(image, wOffset + (wOffset + wCard) * i, offset + hOffset + hShift * j, wCard, hCard);
                    }
                }
            }

            if (dragging || animation)
            {
                string s;

                for (int i = 0; i < buffer.Count; i++)
                {
                    if (buffer[i] != null)
                    {
                        s = GetNameOfPic(buffer[i]);
                        image = (Image)Properties.Resources.ResourceManager.GetObject(s);
                        g.DrawImage(image, x, y + hShift * i, wCard, hCard);
                    }
                }
            }

            // for hint case
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

            // for win case
            if (win)
            {
                g.DrawString("You won!", font, win_brush, wField / 2 - 235, hField / 2 - 30);
            }

        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            ++time;
            toolStripStatusLabel1.Text = "Time: " + time;
        }

        public class Card
        {
            public int suit;   // 1 - clubs, 2 - hearts, 3 - spades, 4 - diamonds
            public int number; // 11 - Jack, 12 - Queen, 13 - King
            public bool opened;

            public Card(int suit, int number, bool opened = false)
            {
                this.suit = suit;
                this.number = number;
                this.opened = opened;
            }
        }

        bool TryStroke(MouseEventArgs e)  // try to make a move 
        {
            // if coordinates are now on the rectangle, check if possible, and if so, move from buffer there
            // check stacks
            for (int i = 0; i < 10; i++)
            {
                int j = stacks[i].Count - 1;

                if (IsIn(e.X, e.Y, wOffset + (wOffset + wCard) * i, offset + hOffset + hShift * j, wCard, hCard))
                {
                    if (stacks[i].Count == 0 || CondNorm(stacks[i][stacks[i].Count - 1], buffer[0]))
                    {
                        countRemember.Add(buffer.Count);   // for undo move
                        oldPlace.Add(candOldPlace);
                        newPlace.Add(i);
                        ToNewPlace();

                        int last = oldPlace.Count - 1;

                        // check if card was opened
                        needToClose.Add(oldPlace[last] < 10 && stacks[oldPlace[last]].Count > 0 && stacks[oldPlace[last]][stacks[oldPlace[last]].Count - 1].opened == false);

                        undoMoveToolStripMenuItem.Enabled = true;

                        return true;
                    }
                    else
                    {
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

        void ToNewPlace()  // put to new place
        {
            Put(newPlace[newPlace.Count - 1]);
        }

        void AddCardsToStacksFromRest()
        {
            /*for (int i = 0; i < 10; i++)
            {
                stacks[i].Add(rest[0]);
                rest.RemoveAt(0);
            }*/
            animation = true;
            num = 0;
            timer3.Start();
            //newGameToolStripMenuItem.Enabled = false;
            returnWas = undoMoveToolStripMenuItem.Enabled;
            undoMoveToolStripMenuItem.Enabled = false;
            hintToolStripMenuItem.Enabled = false;
        }

        void ReturnInAdd()
        {
            for (int i = 9; i >= 0; i--)
            {
                rest.Insert(0, stacks[i][stacks[i].Count - 1]);
                stacks[i].RemoveAt(stacks[i].Count - 1);
            }
        }

        void OpenStacksIfPossible()
        {
            for (int i = 0; i < 10; i++)
            {
                if (stacks[i].Count > 0)
                {
                    stacks[i][stacks[i].Count - 1].opened = true;
                }
            }
        }

        void CanWeTakeIt()   // if we can take the pile, take it
        {
            for (int i = 0; i < 10; i++)
            {
                if (stacks[i].Count >= 13 && stacks[i][stacks[i].Count - 13].opened)
                {
                    // form buffer and check for "normality" =)
                    for (int j = 0; j < 13; j++)
                    {
                        buffer.Add(stacks[i][stacks[i].Count - 13 + j]);
                    }

                    if (NormBuffer())
                    {
                        animation = true;
                        num = i;
                        counter = 0;
                        timer4.Start();
                        undoMoveToolStripMenuItem.Enabled = false;
                        oldPlace.Clear();
                        newPlace.Clear();
                        countRemember.Clear();
                        needToClose.Clear();
                        hintToolStripMenuItem.Enabled = false;

                        // in timer, cards should be sent one by one to the exit

                        // take cards
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

        bool CondNorm(Card c1, Card c2)   // condition for buffer, so cards can be stacked
        {
            return c1.number - c2.number == 1;
        }

        bool NormBuffer()
        {
            for (int i = 0; i < buffer.Count - 1; i++)
            {
                if (!CondNorm(buffer[i], buffer[i + 1]))
                {
                    return false;
                }
            }
            return true;
        }

        bool NormStack(int number)
        {
            int pos = 0;
            while (!stacks[number][pos].opened)
                ++pos;

            for (int i = pos; i < stacks[number].Count - 1; i++)
            {
                if (!CondNorm(stacks[number][i], stacks[number][i + 1]))
                {
                    return false;
                }
            }

            return true;
        }

        int NormStackNumber(int number)  // there will always be at least one normal element in stack
        {
            bool uslNorm;
            int pos = 0;
            while (!stacks[number][pos].opened)
                ++pos;

            for (int i = pos; i < stacks[number].Count - 1; i++)
            {
                uslNorm = true;

                for (int j = i; j < stacks[number].Count - 1; j++)
                {
                    if (!CondNorm(stacks[number][j], stacks[number][j + 1]))
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

        bool Win()    // win condition
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

                // new pile is dealt
                if (quanOfAdds > 0 && IsIn(e.X, e.Y, wField - wOffset - wCard - quanOfAdds * wShift, hField - hOffset - hCard, wCard, hCard))
                {
                    if (IsThereOneEmptyStack())
                    {
                        MessageBox.Show("Each pile must have at least one card", "Message");
                    }
                    else
                    {
                        oldPlace.Add(10);//oldPlace = 10;
                        newPlace.Add(10);//newPlace = 10;
                        countRemember.Add(0);
                        needToClose.Add(false);
                        undoMoveToolStripMenuItem.Enabled = true;
                        AddCardsToStacksFromRest();
                        Invalidate();
                    }
                }

                // check stacks
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
                            dragging = NormBuffer();
                            if (!NormBuffer())
                            {
                                /*GetBack();*/
                                Put(candOldPlace);
                                buffer.Clear();    // maybe this helps??? Seems it did
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
                CanWeTakeIt();
                OpenStacksIfPossible();
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

        private void NewGameToolStripMenuItem_Click(object sender, EventArgs e)
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

            oldPlace.Clear();
            newPlace.Clear();
            countRemember.Clear();
            needToClose.Clear();

            for (int i = 0; i < 104; ++i)
            {
                used[i] = i % 52;
            }

            Shuffle();
            FillStacksAndList();

            hintToolStripMenuItem.Enabled = true;

            timer1.Start();
            undoMoveToolStripMenuItem.Enabled = false;

            Invalidate();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ChangeShirtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 Form2 = new Form2();
            Form2.ShowDialog();
            Invalidate();
        }

        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Solitaire \"Spider\"\nWindows XP\n\nProgrammer: Artur Gumerov", "Help");
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

        private void Timer2_Tick(object sender, EventArgs e)   // cards fall in waves
        {
            h += 2;
            if (h >= 360) h = 0;
            winColor = HsvToRgb(h, 1f, 1f);
            Invalidate();
        }

        private void UndoMoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!dragging)
            {
                accent = -1;
                accent2 = -1;

                int last = oldPlace.Count - 1;

                // if just clicked on the store
                if (oldPlace[last] == 10)
                {
                    ++quanOfAdds;
                    ReturnInAdd();
                }
                else
                {
                    // hide opened card from stack
                    if (needToClose[last])
                    {
                        stacks[oldPlace[last]][stacks[oldPlace[last]].Count - 1].opened = false;
                    }

                    // move countRemember cards from newPlace to oldPlace

                    // I. move countRemember cards from newPlace to buffer

                    for (int i = 0; i < countRemember[last]; i++)
                    {
                        // first remove from new place
                        Card card;

                        card = stacks[newPlace[last]][stacks[newPlace[last]].Count - 1];
                        stacks[newPlace[last]].RemoveAt(stacks[newPlace[last]].Count - 1);

                        // then to buffer
                        buffer.Add(card);
                    }

                    // II. move countRemember cards from buffer to oldPlace

                    for (int i = 0; i < countRemember[last]; i++)
                    {
                        // first remove from buffer
                        Card card = buffer[buffer.Count - 1];
                        buffer.RemoveAt(buffer.Count - 1);

                        // then return to old
                        stacks[oldPlace[last]].Add(card);
                    }
                }

                oldPlace.RemoveAt(last);
                newPlace.RemoveAt(last);
                countRemember.RemoveAt(last);
                needToClose.RemoveAt(last);

                if (oldPlace.Count == 0)
                {
                    undoMoveToolStripMenuItem.Enabled = false;
                }

                Invalidate();
            }
        }

        private void HintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            accentLen = 1;

            // look for suitable moves
            // then want to move from stack to stack (from j to i)
            for (int j = 9; j >= 0; j--)
                if (stacks[j].Count > 0)
                    for (int i = 9; i >= 0; i--)
                    {
                        int k = 0;
                        while (k < stacks[j].Count && !stacks[j][k].opened) ++k;

                        if (i != j && stacks[j].Count > 0 && (stacks[i].Count == 0 || stacks[i].Count > 0 && CondNorm(stacks[i][stacks[i].Count - 1], stacks[j][k])) && NormStack(j))
                        {
                            accentLen = stacks[j].Count - k;
                            if (stacks[i].Count == 0 && k == 0) continue;
                            accent = j;
                            accent2 = i;
                            Invalidate();
                            return;
                        }
                    }

            // didn't work perfectly, let's try simpler
            for (int j = 9; j >= 0; j--)
                if (stacks[j].Count > 0)
                    for (int i = 9; i >= 0; i--)
                    {
                        int k = NormStackNumber(j);
                        //while (k < stacks[j].Count && !stacks[j][k].opened) ++k;

                        if (i != j && stacks[j].Count > 0 && (stacks[i].Count == 0 || stacks[i].Count > 0 && CondNorm(stacks[i][stacks[i].Count - 1], stacks[j][k])))
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
            else MessageBox.Show("No moves", "Message");
            accent2 = -1;
            Invalidate();
        }

        int LinearFunction(int y, int x1, int y1, int x2, int y2)
        {
            return x1 + (y - y1) * (x1 - x2) / (y1 - y2);
        }

        private void Timer3_Tick(object sender, EventArgs e)
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
                    if (y <= offset + hOffset + hShift * stacks[num].Count)
                    {
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
                newGameToolStripMenuItem.Enabled = true;
                undoMoveToolStripMenuItem.Enabled = returnWas;
                hintToolStripMenuItem.Enabled = true;
            }

            Invalidate();
        }

        private void Timer4_Tick(object sender, EventArgs e)
        {
            if (counter < 13)
            {
                if (!animation2)
                {
                    buffer.Add(stacks[num][stacks[num].Count - 1]);
                    stacks[num].RemoveAt(stacks[num].Count - 1);     // take cards
                    x = wOffset + (wOffset + wCard) * num;
                    y = offset + hOffset + hShift * stacks[num].Count;
                    animation2 = true;
                }
                else
                {
                    y += 100;
                    x = LinearFunction(y, wOffset + quanOfPiles * wShift, hField - hOffset - hCard, wOffset + (wOffset + wCard) * num, offset + hOffset + hShift * stacks[num].Count);
                    if (y >= hField - hOffset - hCard)
                    {
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
                newGameToolStripMenuItem.Enabled = true;
                hintToolStripMenuItem.Enabled = true;
                ++quanOfPiles;
                naglCard = null;
                OpenStacksIfPossible();

                if (!win && Win())
                {
                    timer2.Start();
                    win = true;
                    timer1.Stop();
                    undoMoveToolStripMenuItem.Enabled = false;
                    hintToolStripMenuItem.Enabled = false;
                }
            }

            Invalidate();
        }
    }

}
