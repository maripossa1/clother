using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using System.IO.Ports;
using System.Drawing.Printing;

namespace ClothingStoreApp
{
    public class VentesForm : Form
    {
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=clothing_store;Username=postgres;Password=0000;";

        private Panel titleBar;
        private Button closeBtn, minimizeBtn, backBtn;
        private Label titleLabel;

        private TextBox searchBox;
        private DataGridView productsGrid, cartGrid;

        private NumericUpDown quantityBox;
        private Button addButton, removeButton, clearButton, completeSaleButton;
        private Button invoicesButton;

        private Label totalLabel;
        private string currentInvoiceNumber = "";

        private Panel calcPanel;
        private TextBox calcDisplay;
        private string calcOperation = "";
        private double calcFirst = 0;
        private bool calcOpPressed = false;

        private DataTable productsTable = new DataTable();
        private DataTable cartTable = new DataTable();
        private decimal totalAmount = 0;

        // لوحة المفاتيح الافتراضية
        private Panel virtualKeyboard;
        private TextBox currentFocusedTextBox;
        private bool isKeyboardVisible = false;
        private bool isUppercase = false;

        public VentesForm()
        {
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;

            CreateTitleBar();
            CreateLayout();
            CreateVirtualKeyboard();

            InitializeTables();
            LoadProducts();

            productsGrid.DataBindingComplete += ProductsGrid_DataBindingComplete;

            // إضافة معالج حدث التركيز لحقول النص
            AttachFocusHandlers();
        }

        // ===================== لوحة المفاتيح الافتراضية =====================
        private void CreateVirtualKeyboard()
        {
            virtualKeyboard = new Panel()
            {
                Location = new Point(60, 400),
                Size = new Size(900, 350),
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            this.Controls.Add(virtualKeyboard);

            // عنوان لوحة المفاتيح
            Label keyboardTitle = new Label()
            {
                Text = "⌨️ CLAVIER VIRTUEL",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Location = new Point(10, 5),
                AutoSize = true
            };
            virtualKeyboard.Controls.Add(keyboardTitle);

            // زر إغلاق لوحة المفاتيح
            Button closeKeyboardBtn = new Button()
            {
                Text = "✕",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(1150, 5),
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            closeKeyboardBtn.FlatAppearance.BorderSize = 0;
            closeKeyboardBtn.Click += (s, e) => HideVirtualKeyboard();
            virtualKeyboard.Controls.Add(closeKeyboardBtn);

            // إنشاء أزرار لوحة المفاتيح
            CreateKeyboardButtons();
        }

        private void CreateKeyboardButtons()
        {
            int startX = 10;
            int startY = 50;
            int buttonWidth = 60;
            int buttonHeight = 50;
            int spacing = 5;

            // الصف الأول: أرقام
            string[] row1 = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "⌫" };
            CreateKeyboardRow(row1, startX, startY, buttonWidth, buttonHeight, spacing);

            // الصف الثاني: الحروف الأولى
            string[] row2 = { "A", "Z", "E", "R", "T", "Y", "U", "I", "O", "P" };
            CreateKeyboardRow(row2, startX + 30, startY + buttonHeight + spacing, buttonWidth, buttonHeight, spacing);

            // الصف الثالث: الحروف الثانية
            string[] row3 = { "Q", "S", "D", "F", "G", "H", "J", "K", "L", "M" };
            CreateKeyboardRow(row3, startX + 60, startY + 2 * (buttonHeight + spacing), buttonWidth, buttonHeight, spacing);

            // الصف الرابع: الحروف الثالثة وأزرار خاصة
            string[] row4 = { "W", "X", "C", "V", "B", "N", ",", ".", "!", "?" };
            CreateKeyboardRow(row4, startX + 90, startY + 3 * (buttonHeight + spacing), buttonWidth, buttonHeight, spacing);

            // أزرار التحكم
            CreateControlButtons(startX, startY + 4 * (buttonHeight + spacing), buttonWidth, buttonHeight, spacing);
        }

        private void CreateKeyboardRow(string[] keys, int x, int y, int width, int height, int spacing)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                Button keyBtn = new Button()
                {
                    Text = keys[i],
                    Size = new Size(width, height),
                    Location = new Point(x + i * (width + spacing), y),
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    BackColor = Color.White,
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Tag = keys[i]
                };

                keyBtn.FlatAppearance.BorderSize = 1;
                keyBtn.FlatAppearance.BorderColor = Color.Gray;
                keyBtn.Click += VirtualKey_Click;

                virtualKeyboard.Controls.Add(keyBtn);
            }
        }

        private void CreateControlButtons(int x, int y, int width, int height, int spacing)
        {
            // زر Shift
            Button shiftBtn = new Button()
            {
                Text = "⇧ MAJ",
                Size = new Size(width * 2, height),
                Location = new Point(x, y),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            shiftBtn.FlatAppearance.BorderSize = 1;
            shiftBtn.Click += ShiftBtn_Click;
            virtualKeyboard.Controls.Add(shiftBtn);

            // زر المسافة
            Button spaceBtn = new Button()
            {
                Text = "ESPACE",
                Size = new Size(width * 6, height),
                Location = new Point(x + width * 2 + spacing, y),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Tag = " "
            };
            spaceBtn.FlatAppearance.BorderSize = 1;
            spaceBtn.Click += VirtualKey_Click;
            virtualKeyboard.Controls.Add(spaceBtn);

            // زر الإدخال
            Button enterBtn = new Button()
            {
                Text = "ENTRER",
                Size = new Size(width * 2, height),
                Location = new Point(x + width * 8 + spacing * 2, y),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = "ENTER"
            };
            enterBtn.FlatAppearance.BorderSize = 1;
            enterBtn.Click += VirtualKey_Click;
            virtualKeyboard.Controls.Add(enterBtn);

            // زر المسح
            Button clearBtn = new Button()
            {
                Text = "EFFACER",
                Size = new Size(width * 2, height),
                Location = new Point(x + width * 10 + spacing * 3, y),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = "CLEAR"
            };
            clearBtn.FlatAppearance.BorderSize = 1;
            clearBtn.Click += VirtualKey_Click;
            virtualKeyboard.Controls.Add(clearBtn);
        }

        private void VirtualKey_Click(object sender, EventArgs e)
        {
            Button keyBtn = sender as Button;
            if (keyBtn == null || keyBtn.Tag == null) return;

            string key = keyBtn.Tag.ToString();

            if (currentFocusedTextBox != null)
            {
                switch (key)
                {
                    case "⌫": // Backspace
                        if (currentFocusedTextBox.Text.Length > 0)
                        {
                            currentFocusedTextBox.Text = currentFocusedTextBox.Text.Substring(0, currentFocusedTextBox.Text.Length - 1);
                        }
                        break;

                    case "ENTER": // Enter
                        HideVirtualKeyboard();
                        break;

                    case "CLEAR": // Clear
                        currentFocusedTextBox.Text = "";
                        break;

                    default:
                        string inputChar = isUppercase ? key : key.ToLower();
                        currentFocusedTextBox.Text += inputChar;
                        break;
                }
            }
        }

        private void ShiftBtn_Click(object sender, EventArgs e)
        {
            isUppercase = !isUppercase;
            Button shiftBtn = sender as Button;

            // تحديث جميع أزرار الحروف
            foreach (Control control in virtualKeyboard.Controls)
            {
                if (control is Button btn && btn.Tag != null)
                {
                    string tag = btn.Tag.ToString();
                    if (tag.Length == 1 && char.IsLetter(tag[0]))
                    {
                        btn.Text = isUppercase ? tag.ToUpper() : tag.ToLower();
                    }
                }
            }

            shiftBtn.BackColor = isUppercase ? Color.FromArgb(0, 123, 255) : Color.FromArgb(100, 100, 100);
        }

        private void ShowVirtualKeyboard(TextBox textBox)
        {
            currentFocusedTextBox = textBox;
            virtualKeyboard.Visible = true;
            virtualKeyboard.BringToFront();
            isKeyboardVisible = true;
        }

        private void HideVirtualKeyboard()
        {
            virtualKeyboard.Visible = false;
            currentFocusedTextBox = null;
            isKeyboardVisible = false;
        }

        // ===================== إضافة معالجات التركيز للحقول =====================
        private void AttachFocusHandlers()
        {
            // لحقل البحث
            searchBox.GotFocus += (s, e) => ShowVirtualKeyboard(searchBox);
            searchBox.Click += (s, e) => ShowVirtualKeyboard(searchBox);

            // للآلة الحاسبة
            if (calcDisplay != null)
            {
                calcDisplay.GotFocus += (s, e) => ShowVirtualKeyboard(calcDisplay);
                calcDisplay.Click += (s, e) => ShowVirtualKeyboard(calcDisplay);
            }
        }

        private void CreateTitleBar()
        {
            titleBar = new Panel()
            {
                BackColor = Color.Black,
                Height = 80,
                Dock = DockStyle.Top
            };

            backBtn = new Button()
            {
                Text = "←",
                Font = new Font("Arial", 26, FontStyle.Bold),
                Size = new Size(70, 70),
                Location = new Point(10, 5),
                BackColor = Color.FromArgb(65, 179, 163),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            backBtn.FlatAppearance.BorderSize = 0;
            backBtn.Click += (_, _) => this.Close();

            minimizeBtn = new Button()
            {
                Text = "_",
                Font = new Font("Arial", 26, FontStyle.Bold),
                Size = new Size(70, 70),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            minimizeBtn.FlatAppearance.BorderSize = 0;
            minimizeBtn.Click += (_, _) => this.WindowState = FormWindowState.Minimized;

            closeBtn = new Button()
            {
                Text = "X",
                Font = new Font("Arial", 26, FontStyle.Bold),
                Size = new Size(70, 70),
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (_, _) => Application.Exit();

            titleLabel = new Label()
            {
                Text = "VENTES - TOUCH",
                Font = new Font("Arial", 26, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            titleBar.Controls.Add(backBtn);
            titleBar.Controls.Add(minimizeBtn);
            titleBar.Controls.Add(closeBtn);
            titleBar.Controls.Add(titleLabel);
            this.Controls.Add(titleBar);

            titleBar.Resize += (_, _) =>
            {
                minimizeBtn.Location = new Point(titleBar.Width - 150, 5);
                closeBtn.Location = new Point(titleBar.Width - 75, 5);
                titleLabel.Location = new Point((titleBar.Width - titleLabel.Width) / 2, 20);
            };
        }

        private void CreateLayout()
        {
            // BARRE DE RECHERCHE
            searchBox = new TextBox()
            {
                Font = new Font("Arial", 22, FontStyle.Bold),
                PlaceholderText = "🔍 Rechercher par nom, code ou prix...",
                Location = new Point(20, 100),
                Size = new Size(700, 50),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            searchBox.TextChanged += SearchBox_TextChanged;
            this.Controls.Add(searchBox);

            // زر إظهار لوحة المفاتيح للبحث
            Button searchKeyboardBtn = new Button()
            {
                Text = "⌨️",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(60, 50),
                Location = new Point(730, 100),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            searchKeyboardBtn.FlatAppearance.BorderSize = 0;
            searchKeyboardBtn.Click += (s, e) => ShowVirtualKeyboard(searchBox);
            this.Controls.Add(searchKeyboardBtn);

            // LISTE DES PRODUITS
            productsGrid = new DataGridView()
            {
                Location = new Point(20, 160),
                Size = new Size(900, 600),
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                Font = new Font("Arial", 14),
                RowTemplate = { Height = 45 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false
            };
            productsGrid.DoubleClick += ProductsGrid_DoubleClick;
            this.Controls.Add(productsGrid);

            // AJOUT AU PANIER
            GroupBox addBox = new GroupBox()
            {
                Text = "➕ AJOUT AU PANIER",
                Font = new Font("Arial", 18, FontStyle.Bold),
                Location = new Point(950, 140),
                Size = new Size(450, 200)
            };
            this.Controls.Add(addBox);

            Label lblQty = new Label()
            {
                Text = "Quantité :",
                Font = new Font("Arial", 18),
                Location = new Point(20, 40),
                AutoSize = true
            };
            addBox.Controls.Add(lblQty);

            quantityBox = new NumericUpDown()
            {
                Minimum = 1,
                Maximum = 999,
                Value = 1,
                Font = new Font("Arial", 20, FontStyle.Bold),
                Location = new Point(180, 35),
                Size = new Size(120, 50),
                TextAlign = HorizontalAlignment.Center
            };
            addBox.Controls.Add(quantityBox);

            // أزرار سريعة للكمية
            Button qtyPlusBtn = new Button()
            {
                Text = "+",
                Font = new Font("Arial", 20, FontStyle.Bold),
                Size = new Size(50, 50),
                Location = new Point(310, 35),
                BackColor = Color.FromArgb(30, 180, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            qtyPlusBtn.FlatAppearance.BorderSize = 0;
            qtyPlusBtn.Click += (s, e) => quantityBox.Value = Math.Min(quantityBox.Maximum, quantityBox.Value + 1);
            addBox.Controls.Add(qtyPlusBtn);

            Button qtyMinusBtn = new Button()
            {
                Text = "-",
                Font = new Font("Arial", 20, FontStyle.Bold),
                Size = new Size(50, 50),
                Location = new Point(370, 35),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            qtyMinusBtn.FlatAppearance.BorderSize = 0;
            qtyMinusBtn.Click += (s, e) => quantityBox.Value = Math.Max(quantityBox.Minimum, quantityBox.Value - 1);
            addBox.Controls.Add(qtyMinusBtn);

            addButton = new Button()
            {
                Text = "🛒 AJOUTER",
                Font = new Font("Arial", 20, FontStyle.Bold),
                Size = new Size(380, 60),
                BackColor = Color.FromArgb(30, 180, 80),
                ForeColor = Color.White,
                Location = new Point(20, 100),
                FlatStyle = FlatStyle.Flat
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.Click += AddButton_Click;
            addBox.Controls.Add(addButton);

            // PAIEMENT
            GroupBox payBox = new GroupBox()
            {
                Text = "💳 PAIEMENT",
                Font = new Font("Arial", 18, FontStyle.Bold),
                Location = new Point(950, 380),
                Size = new Size(450, 250)
            };
            this.Controls.Add(payBox);

            totalLabel = new Label()
            {
                Text = "Total : 0.00 €",
                Font = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.Green,
                AutoSize = true,
                Location = new Point(20, 90)
            };
            payBox.Controls.Add(totalLabel);

            // BOUTONS D'ACTION
            removeButton = new Button()
            {
                Text = "❌ RETIRER",
                Font = new Font("Arial", 18, FontStyle.Bold),
                Size = new Size(200, 60),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Location = new Point(950, 650),
                FlatStyle = FlatStyle.Flat
            };
            removeButton.FlatAppearance.BorderSize = 0;
            removeButton.Click += RemoveButton_Click;
            this.Controls.Add(removeButton);

            clearButton = new Button()
            {
                Text = "🗑️ VIDER",
                Font = new Font("Arial", 18, FontStyle.Bold),
                Size = new Size(200, 60),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Location = new Point(1200, 650),
                FlatStyle = FlatStyle.Flat
            };
            clearButton.FlatAppearance.BorderSize = 0;
            clearButton.Click += ClearButton_Click;
            this.Controls.Add(clearButton);

            // BOUTON TERMINER VENTE
            completeSaleButton = new Button()
            {
                Text = "💰 TERMINER VENTE",
                Font = new Font("Arial", 20, FontStyle.Bold),
                Size = new Size(450, 70),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                Location = new Point(950, 730),
                FlatStyle = FlatStyle.Flat
            };
            completeSaleButton.FlatAppearance.BorderSize = 0;
            completeSaleButton.Click += CompleteSaleButton_Click;
            this.Controls.Add(completeSaleButton);

            // BOUTON TVA RAPIDE
            Button tvaButton = new Button()
            {
                Text = "TVA 20% ✅",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(450, 50),
                Location = new Point(950, 810),
                BackColor = Color.FromArgb(30, 180, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = 20.00m
            };
            tvaButton.FlatAppearance.BorderSize = 0;
            tvaButton.Click += TvaButton_Click;
            this.Controls.Add(tvaButton);

            // BOUTON FACTURES
            invoicesButton = new Button()
            {
                Text = "🧾 LISTE FACTURES",
                Font = new Font("Arial", 18, FontStyle.Bold),
                Size = new Size(450, 60),
                BackColor = Color.FromArgb(147, 112, 219),
                ForeColor = Color.White,
                Location = new Point(950, 870),
                FlatStyle = FlatStyle.Flat
            };
            invoicesButton.FlatAppearance.BorderSize = 0;
            invoicesButton.Click += InvoicesButton_Click;
            this.Controls.Add(invoicesButton);

            // BOUTON OUVERTURE TIROIR-CAISSE
            Button openDrawerBtn = new Button()
            {
                Text = "💰 OUVRIER TIROIR",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(450, 50),
                Location = new Point(950, 930),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            openDrawerBtn.FlatAppearance.BorderSize = 0;
            openDrawerBtn.Click += (s, e) => OpenCashDrawer();
            this.Controls.Add(openDrawerBtn);

            // GRILLE PANIER
            cartGrid = new DataGridView()
            {
                Location = new Point(20, 780),
                Size = new Size(900, 280),
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                Font = new Font("Arial", 12),
                RowTemplate = { Height = 40 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false
            };
            this.Controls.Add(cartGrid);

            // CALCULATRICE
            CreateCalculator();
        }

        // ===================== ÉVÉNEMENT BOUTON TVA =====================
        private void TvaButton_Click(object sender, EventArgs e)
        {
            Button tvaBtn = sender as Button;
            decimal currentRate = (decimal)tvaBtn.Tag;

            // تبديل بين TVA 20% و 0%
            if (currentRate == 20.00m)
            {
                tvaBtn.Tag = 0.00m;
                tvaBtn.Text = "TVA 0% ❌";
                tvaBtn.BackColor = Color.Red;
            }
            else
            {
                tvaBtn.Tag = 20.00m;
                tvaBtn.Text = "TVA 20% ✅";
                tvaBtn.BackColor = Color.FromArgb(30, 180, 80);
            }
        }

        private void CreateCalculator()
        {
            GroupBox calcGroup = new GroupBox()
            {
                Text = "🧮 CALCULATRICE VENTE RAPIDE",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(1420, 160),
                Size = new Size(500, 650),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };
            this.Controls.Add(calcGroup);

            calcPanel = new Panel()
            {
                Location = new Point(15, 45),
                Size = new Size(470, 640),
                BackColor = Color.FromArgb(45, 45, 48)
            };
            calcGroup.Controls.Add(calcPanel);

            calcDisplay = new TextBox()
            {
                Location = new Point(10, 10),
                Size = new Size(450, 70),
                Font = new Font("Arial", 28, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Right,
                ReadOnly = true,
                Text = "0",
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Lime,
                BorderStyle = BorderStyle.FixedSingle
            };
            calcPanel.Controls.Add(calcDisplay);

            var buttons = new[]
            {
                new { Text = "C", Row = 1, Col = 0, Color = Color.FromArgb(255, 80, 80), Width = 1 },
                new { Text = "CE", Row = 1, Col = 1, Color = Color.FromArgb(255, 120, 120), Width = 1 },
                new { Text = "±", Row = 1, Col = 2, Color = Color.FromArgb(100, 100, 100), Width = 1 },
                new { Text = "÷", Row = 1, Col = 3, Color = Color.FromArgb(255, 159, 0), Width = 1 },

                new { Text = "7", Row = 2, Col = 0, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "8", Row = 2, Col = 1, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "9", Row = 2, Col = 2, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "×", Row = 2, Col = 3, Color = Color.FromArgb(255, 159, 0), Width = 1 },

                new { Text = "4", Row = 3, Col = 0, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "5", Row = 3, Col = 1, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "6", Row = 3, Col = 2, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "-", Row = 3, Col = 3, Color = Color.FromArgb(255, 159, 0), Width = 1 },

                new { Text = "1", Row = 4, Col = 0, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "2", Row = 4, Col = 1, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "3", Row = 4, Col = 2, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "+", Row = 4, Col = 3, Color = Color.FromArgb(255, 159, 0), Width = 1 },

                new { Text = "0", Row = 5, Col = 0, Color = Color.FromArgb(70, 70, 70), Width = 2 },
                new { Text = ".", Row = 5, Col = 2, Color = Color.FromArgb(70, 70, 70), Width = 1 },
                new { Text = "=", Row = 5, Col = 3, Color = Color.FromArgb(255, 159, 0), Width = 1 }
            };

            int buttonWidth = 85;
            int buttonHeight = 60;
            int spacing = 10;
            int startX = 10;
            int startY = 90;

            foreach (var btnInfo in buttons)
            {
                int width = btnInfo.Width == 2 ? (buttonWidth * 2 + spacing) : buttonWidth;

                Button btn = new Button()
                {
                    Text = btnInfo.Text,
                    Size = new Size(width, buttonHeight),
                    Location = new Point(startX + btnInfo.Col * (buttonWidth + spacing),
                                       startY + (btnInfo.Row - 1) * (buttonHeight + spacing)),
                    Font = new Font("Arial", 18, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Tag = btnInfo.Text,
                    BackColor = btnInfo.Color,
                    ForeColor = Color.White
                };

                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(btnInfo.Color, 0.2f);
                btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(btnInfo.Color, 0.3f);
                btn.Click += CalculatorButton_Click;

                calcPanel.Controls.Add(btn);
            }

            // زر لوحة المفاتيح للآلة الحاسبة
            Button calcKeyboardBtn = new Button()
            {
                Text = "⌨️",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(80, 40),
                Location = new Point(380, 10),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            calcKeyboardBtn.FlatAppearance.BorderSize = 0;
            calcKeyboardBtn.Click += (s, e) => ShowVirtualKeyboard(calcDisplay);
            calcPanel.Controls.Add(calcKeyboardBtn);

            // Bouton ajouter au panier
            Button addToCartBtn = new Button()
            {
                Text = "AJOUTER PANIER",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(380, 60),
                Location = new Point(10, 90 + 5 * (buttonHeight + spacing)),
                BackColor = Color.FromArgb(30, 180, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            addToCartBtn.FlatAppearance.BorderSize = 0;
            addToCartBtn.Click += AddCalculatorToCart;
            calcPanel.Controls.Add(addToCartBtn);

            // Bouton vente rapide
            Button venteRapideBtn = new Button()
            {
                Text = "💰 VENTE RAPIDE",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(380, 90),
                Location = new Point(10, 90 + 6 * (buttonHeight + spacing)),
                BackColor = Color.FromArgb(147, 112, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            venteRapideBtn.FlatAppearance.BorderSize = 0;
            venteRapideBtn.Click += VenteRapideButton_Click;
            calcPanel.Controls.Add(venteRapideBtn);
        }

        // ===================== VENTE RAPIDE =====================
        private void VenteRapideButton_Click(object sender, EventArgs e)
        {
            ShowVenteRapideForm();
        }

        private void ShowVenteRapideForm()
        {
            string defaultPrice = "0.00";
            if (double.TryParse(calcDisplay.Text, out double calcResult) && calcResult > 0)
            {
                defaultPrice = calcResult.ToString("F2");
            }

            Form venteRapideForm = new Form()
            {
                Text = "💰 Vente Rapide",
                Size = new Size(500, 350),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            };

            Label titleLabel = new Label()
            {
                Text = "VENTE RAPIDE - Produit Non Répertorié",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Label nomLabel = new Label()
            {
                Text = "Nom produit:",
                Font = new Font("Arial", 14),
                Location = new Point(20, 70),
                AutoSize = true
            };

            TextBox nomTextBox = new TextBox()
            {
                Font = new Font("Arial", 14),
                Location = new Point(180, 65),
                Size = new Size(280, 35),
                Text = "Vente Rapide"
            };

            Label prixLabel = new Label()
            {
                Text = "Prix:",
                Font = new Font("Arial", 14),
                Location = new Point(20, 120),
                AutoSize = true
            };

            TextBox prixTextBox = new TextBox()
            {
                Font = new Font("Arial", 14),
                Location = new Point(180, 115),
                Size = new Size(150, 35),
                Text = defaultPrice
            };

            Label qtyLabel = new Label()
            {
                Text = "Quantité:",
                Font = new Font("Arial", 14),
                Location = new Point(20, 170),
                AutoSize = true
            };

            NumericUpDown qtyBox = new NumericUpDown()
            {
                Font = new Font("Arial", 14),
                Location = new Point(180, 165),
                Size = new Size(150, 35),
                Minimum = 1,
                Maximum = 999,
                Value = 1
            };

            Button ajouterBtn = new Button()
            {
                Text = "🛒 AJOUTER AU PANIER",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(250, 50),
                BackColor = Color.FromArgb(30, 180, 80),
                ForeColor = Color.White,
                Location = new Point(20, 220),
                FlatStyle = FlatStyle.Flat
            };
            ajouterBtn.FlatAppearance.BorderSize = 0;
            ajouterBtn.Click += (s, e) =>
            {
                if (!decimal.TryParse(prixTextBox.Text, out decimal prix) || prix <= 0)
                {
                    MessageBox.Show("Prix invalide.", "Erreur");
                    return;
                }

                string nomProduit = string.IsNullOrWhiteSpace(nomTextBox.Text) ?
                                   $"Vente Rapide {prix:F2}€" : nomTextBox.Text;

                int qty = (int)qtyBox.Value;
                decimal total = prix * qty;

                cartTable.Rows.Add(0, nomProduit, prix, qty, total);
                UpdateCartSummary();
                venteRapideForm.Close();
            };

            Button annulerBtn = new Button()
            {
                Text = "Annuler",
                Font = new Font("Arial", 14),
                Size = new Size(150, 50),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Location = new Point(290, 220),
                FlatStyle = FlatStyle.Flat
            };
            annulerBtn.FlatAppearance.BorderSize = 0;
            annulerBtn.Click += (s, e) => venteRapideForm.Close();

            venteRapideForm.Controls.Add(titleLabel);
            venteRapideForm.Controls.Add(nomLabel);
            venteRapideForm.Controls.Add(nomTextBox);
            venteRapideForm.Controls.Add(prixLabel);
            venteRapideForm.Controls.Add(prixTextBox);
            venteRapideForm.Controls.Add(qtyLabel);
            venteRapideForm.Controls.Add(qtyBox);
            venteRapideForm.Controls.Add(ajouterBtn);
            venteRapideForm.Controls.Add(annulerBtn);

            venteRapideForm.ShowDialog();
        }

        // ===================== Ajout direct depuis la calculatrice au panier =====================
        private void AddCalculatorToCart(object sender, EventArgs e)
        {
            if (double.TryParse(calcDisplay.Text, out double result) && result > 0)
            {
                decimal unitPrice = (decimal)result;
                int quantity = 1;
                decimal total = unitPrice * quantity;

                cartTable.Rows.Add(0, $"Produit rapide {unitPrice:F2}€", unitPrice, quantity, total);
                UpdateCartSummary();

                calcDisplay.Text = "0";
                calcFirst = 0;
                calcOperation = "";
                calcOpPressed = false;
            }
        }

        // ===================== LOGIQUE CALCULATRICE =====================
        private void CalculatorButton_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;
            if (b == null || b.Tag == null) return;
            string k = b.Tag.ToString();

            if (decimal.TryParse(k, out _) || k == "00" || k == ".")
            {
                if (k == ".")
                {
                    if (!calcDisplay.Text.Contains("."))
                        calcDisplay.Text += ".";
                }
                else if (k == "00")
                {
                    if (calcDisplay.Text == "0" || calcOpPressed)
                        calcDisplay.Text = "0";
                    else
                        calcDisplay.Text += "00";
                }
                else
                {
                    if (calcOpPressed || calcDisplay.Text == "0")
                        calcDisplay.Text = k;
                    else
                        calcDisplay.Text += k;
                }
                calcOpPressed = false;
                return;
            }

            switch (k)
            {
                case "C":
                    calcDisplay.Text = "0";
                    calcFirst = 0;
                    calcOperation = "";
                    calcOpPressed = false;
                    break;

                case "CE":
                    calcDisplay.Text = "0";
                    calcOpPressed = false;
                    break;

                case "±":
                    if (calcDisplay.Text.StartsWith("-"))
                        calcDisplay.Text = calcDisplay.Text.Substring(1);
                    else if (calcDisplay.Text != "0")
                        calcDisplay.Text = "-" + calcDisplay.Text;
                    break;

                case "+":
                case "-":
                case "×":
                case "÷":
                    if (!string.IsNullOrEmpty(calcOperation))
                        CalculatorApply();
                    calcFirst = double.Parse(calcDisplay.Text);
                    calcOperation = k;
                    calcOpPressed = true;
                    break;

                case "=":
                    CalculatorApply();
                    calcOperation = "";
                    break;
            }
        }

        private void CalculatorApply()
        {
            try
            {
                double second = double.Parse(calcDisplay.Text);
                switch (calcOperation)
                {
                    case "+": calcFirst += second; break;
                    case "-": calcFirst -= second; break;
                    case "×": calcFirst *= second; break;
                    case "÷":
                        if (second != 0)
                            calcFirst /= second;
                        else
                        {
                            calcDisplay.Text = "Error";
                            return;
                        }
                        break;
                }
                calcDisplay.Text = calcFirst.ToString("F2");
                calcOpPressed = true;
            }
            catch
            {
                calcDisplay.Text = "Error";
            }
        }

        private void InitializeTables()
        {
            productsTable = new DataTable();

            cartTable = new DataTable();
            cartTable.Columns.Add("ID", typeof(int));
            cartTable.Columns.Add("PRODUIT", typeof(string));
            cartTable.Columns.Add("PRIX", typeof(decimal));
            cartTable.Columns.Add("QUANTITE", typeof(int));
            cartTable.Columns.Add("TOTAL", typeof(decimal));

            cartGrid.DataSource = cartTable;
            productsGrid.DataSource = productsTable;
        }

        private void LoadProducts()
        {
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                string query = @"SELECT 
                                    barcode AS CODE_BARRE,
                                    name AS PRODUIT,
                                    category AS CATEGORIE,
                                    price AS PRIX,
                                    id AS ID,
                                    stock_quantity AS STOCK
                                FROM products
                                WHERE stock_quantity > 0
                                ORDER BY name, barcode";

                using var da = new NpgsqlDataAdapter(query, con);

                productsTable.Clear();
                da.Fill(productsTable);

                productsGrid.DataSource = null;
                productsGrid.DataSource = productsTable;

                FormatDataGrids();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement des produits: {ex.Message}", "Erreur");
            }
        }

        private void FormatDataGrids()
        {
            try
            {
                if (productsGrid.Columns.Count > 0)
                {
                    foreach (DataGridViewColumn col in productsGrid.Columns)
                    {
                        col.Visible = false;
                    }

                    string[] visibleColumns = { "CODE_BARRE", "PRODUIT", "CATEGORIE", "PRIX", "STOCK" };

                    foreach (string colName in visibleColumns)
                    {
                        if (productsGrid.Columns.Contains(colName))
                        {
                            var col = productsGrid.Columns[colName];
                            col.Visible = true;

                            switch (colName)
                            {
                                case "CODE_BARRE":
                                    col.HeaderText = "CODE";
                                    col.Width = 150;
                                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                    col.DefaultCellStyle.Font = new Font("Arial", 10);
                                    col.DefaultCellStyle.ForeColor = Color.Gray;
                                    break;
                                case "PRODUIT":
                                    col.HeaderText = "NOM DU PRODUIT";
                                    col.Width = 350;
                                    col.DefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
                                    col.DefaultCellStyle.ForeColor = Color.FromArgb(0, 0, 139);
                                    break;
                                case "CATEGORIE":
                                    col.HeaderText = "CATÉGORIE";
                                    col.Width = 150;
                                    col.DefaultCellStyle.Font = new Font("Arial", 10);
                                    break;
                                case "PRIX":
                                    col.HeaderText = "PRIX (€)";
                                    col.Width = 120;
                                    col.DefaultCellStyle.Format = "N2";
                                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                                    col.DefaultCellStyle.Font = new Font("Arial", 11, FontStyle.Bold);
                                    col.DefaultCellStyle.ForeColor = Color.Green;
                                    break;
                                case "STOCK":
                                    col.HeaderText = "QUANTITÉ";
                                    col.Width = 100;
                                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                    col.DefaultCellStyle.Font = new Font("Arial", 11);
                                    break;
                            }
                        }
                    }

                    ApplyStockColorCoding();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Format error: {ex.Message}");
            }

            FormatCartGrid();
        }

        private void ApplyStockColorCoding()
        {
            try
            {
                foreach (DataGridViewRow row in productsGrid.Rows)
                {
                    if (row.IsNewRow) continue;

                    if (row.Cells["STOCK"]?.Value != null && int.TryParse(row.Cells["STOCK"].Value.ToString(), out int stock))
                    {
                        if (stock == 0)
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                            row.DefaultCellStyle.ForeColor = Color.FromArgb(200, 0, 0);
                        }
                        else if (stock <= 5)
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 230);
                            row.DefaultCellStyle.ForeColor = Color.FromArgb(200, 100, 0);
                        }
                        else
                        {
                            row.DefaultCellStyle.BackColor = Color.White;
                            row.DefaultCellStyle.ForeColor = Color.Black;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur color coding: {ex.Message}");
            }
        }

        private void FormatCartGrid()
        {
            try
            {
                if (cartGrid.Columns.Count > 0)
                {
                    cartGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                    cartGrid.RowHeadersVisible = false;
                    cartGrid.AllowUserToAddRows = false;

                    var cartColumnsConfig = new[]
                    {
                        new { Name = "PRODUIT", Header = "PRODUIT", Width = 400, Alignment = HorizontalAlignment.Left },
                        new { Name = "PRIX", Header = "PRIX (€)", Width = 120, Alignment = HorizontalAlignment.Right },
                        new { Name = "QUANTITE", Header = "QUANTITÉ", Width = 100, Alignment = HorizontalAlignment.Center },
                        new { Name = "TOTAL", Header = "TOTAL (€)", Width = 120, Alignment = HorizontalAlignment.Right }
                    };

                    foreach (var colInfo in cartColumnsConfig)
                    {
                        if (cartGrid.Columns.Contains(colInfo.Name))
                        {
                            var col = cartGrid.Columns[colInfo.Name];
                            col.Visible = true;
                            col.HeaderText = colInfo.Header;
                            col.Width = colInfo.Width;
                            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

                            switch (colInfo.Name)
                            {
                                case "PRODUIT":
                                    col.DefaultCellStyle.Font = new Font("Arial", 11, FontStyle.Bold);
                                    col.DefaultCellStyle.ForeColor = Color.FromArgb(0, 0, 139);
                                    break;
                                case "PRIX":
                                case "TOTAL":
                                    col.DefaultCellStyle.Format = "N2";
                                    col.DefaultCellStyle.Font = new Font("Arial", 11, FontStyle.Bold);
                                    col.DefaultCellStyle.ForeColor = Color.Green;
                                    break;
                                case "QUANTITE":
                                    col.DefaultCellStyle.Font = new Font("Arial", 11, FontStyle.Bold);
                                    col.DefaultCellStyle.ForeColor = Color.Black;
                                    break;
                            }

                            switch (colInfo.Alignment)
                            {
                                case HorizontalAlignment.Center:
                                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                    break;
                                case HorizontalAlignment.Right:
                                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                                    break;
                                default:
                                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                                    break;
                            }
                        }
                    }

                    if (cartGrid.Columns.Contains("ID"))
                    {
                        cartGrid.Columns["ID"].Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cart format error: {ex.Message}");
            }
        }

        private void SearchProducts(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                LoadProducts();
                return;
            }

            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                string query;
                NpgsqlCommand cmd;

                if (decimal.TryParse(keyword, out decimal priceSearch))
                {
                    query = @"SELECT 
                                barcode AS CODE_BARRE,
                                name AS PRODUIT,
                                category AS CATEGORIE,
                                price AS PRIX,
                                id AS ID,
                                stock_quantity AS STOCK
                            FROM products
                            WHERE stock_quantity > 0
                              AND price = @price
                            ORDER BY name";

                    cmd = new NpgsqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@price", priceSearch);
                }
                else
                {
                    query = @"SELECT 
                                barcode AS CODE_BARRE,
                                name AS PRODUIT,
                                category AS CATEGORIE,
                                price AS PRIX,
                                id AS ID,
                                stock_quantity AS STOCK
                            FROM products
                            WHERE stock_quantity > 0
                              AND (LOWER(name) LIKE LOWER(@kw)
                               OR LOWER(barcode) LIKE LOWER(@kw)
                               OR LOWER(category) LIKE LOWER(@kw))
                            ORDER BY name";

                    cmd = new NpgsqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                }

                using var da = new NpgsqlDataAdapter(cmd);

                DataTable searchTable = new DataTable();
                da.Fill(searchTable);

                productsTable.Clear();
                productsTable = searchTable.Copy();

                productsGrid.DataSource = null;
                productsGrid.DataSource = productsTable;
                FormatDataGrids();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche: {ex.Message}", "Erreur");
            }
        }

        private void AddToCart()
        {
            try
            {
                if (productsGrid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Veuillez sélectionner un produit.", "Info");
                    return;
                }

                DataGridViewRow sel = productsGrid.SelectedRows[0];
                int productId = Convert.ToInt32(sel.Cells["ID"].Value);
                string name = sel.Cells["PRODUIT"].Value?.ToString() ?? "";
                decimal price = Convert.ToDecimal(sel.Cells["PRIX"].Value);
                int stock = Convert.ToInt32(sel.Cells["STOCK"].Value);
                int qty = (int)quantityBox.Value;

                if (qty <= 0) { MessageBox.Show("Quantité invalide.", "Erreur"); return; }
                if (qty > stock) { MessageBox.Show($"Stock insuffisant. Il reste {stock}.", "Erreur"); return; }

                foreach (DataRow row in cartTable.Rows)
                {
                    if (Convert.ToInt32(row["ID"]) == productId)
                    {
                        int cur = Convert.ToInt32(row["QUANTITE"]);
                        int nw = cur + qty;
                        if (nw > stock) { MessageBox.Show($"Quantité totale ({nw}) dépasse le stock ({stock})", "Erreur"); return; }
                        row["QUANTITE"] = nw;
                        row["TOTAL"] = Convert.ToDecimal(row["PRIX"]) * nw;
                        UpdateCartSummary();
                        return;
                    }
                }

                decimal total = price * qty;
                cartTable.Rows.Add(productId, name, price, qty, total);
                UpdateCartSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur ajout au panier: {ex.Message}", "Erreur");
            }
        }

        private void RemoveFromCart()
        {
            try
            {
                if (cartGrid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Sélectionnez un article à retirer.", "Info");
                    return;
                }

                foreach (DataGridViewRow row in cartGrid.SelectedRows)
                {
                    cartTable.Rows.RemoveAt(row.Index);
                }
                UpdateCartSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur retrait: {ex.Message}", "Erreur");
            }
        }

        private void ClearCart()
        {
            if (cartTable.Rows.Count == 0) return;
            if (MessageBox.Show("Vider tout le panier ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                cartTable.Rows.Clear();
                UpdateCartSummary();
            }
        }

        private void UpdateCartSummary()
        {
            totalAmount = 0;
            foreach (DataRow row in cartTable.Rows)
            {
                totalAmount += Convert.ToDecimal(row["TOTAL"]);
            }
            totalLabel.Text = $"Total : {totalAmount:N2} €";
        }

        private void CompleteSaleButton_Click(object sender, EventArgs e)
        {
            if (cartTable.Rows.Count == 0)
            {
                MessageBox.Show("Le panier est vide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // الحصول على قيمة TVA من الزر
            Button tvaButton = this.Controls.OfType<Button>()
                .FirstOrDefault(b => b.Text.Contains("TVA"));

            decimal tvaRate = 20.00m; // افتراضي
            if (tvaButton != null && tvaButton.Tag != null)
            {
                tvaRate = (decimal)tvaButton.Tag;
            }

            using var con = new NpgsqlConnection(connectionString);
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                DateTime saleTime = DateTime.Now;
                List<int> generatedInvoiceIds = new List<int>();

                foreach (DataRow row in cartTable.Rows)
                {
                    int id = Convert.ToInt32(row["ID"]);
                    int qty = Convert.ToInt32(row["QUANTITE"]);
                    string productName = row["PRODUIT"].ToString();
                    decimal price = Convert.ToDecimal(row["PRIX"]);
                    decimal total = Convert.ToDecimal(row["TOTAL"]);

                    // حساب TVA
                    decimal unitPriceWithoutTva = price / (1 + tvaRate / 100);
                    decimal totalWithoutTva = total / (1 + tvaRate / 100);

                    if (id > 0)
                    {
                        string updateStockQuery = @"UPDATE products 
                         SET stock_quantity = stock_quantity - @q, 
                             updated_at = CURRENT_TIMESTAMP 
                         WHERE id = @id AND stock_quantity >= @q";
                        using var updateCmd = new NpgsqlCommand(updateStockQuery, con, tx);
                        updateCmd.Parameters.AddWithValue("@q", qty);
                        updateCmd.Parameters.AddWithValue("@id", id);

                        int affected = updateCmd.ExecuteNonQuery();
                        if (affected == 0)
                        {
                            tx.Rollback();
                            MessageBox.Show($"Échec: stock insuffisant pour le produit {productName}.", "Erreur");
                            return;
                        }
                    }

                    string insertSaleQuery = @"INSERT INTO sales 
                    (product_name, quantity_sold, unit_price, total_amount, sale_date,
                     tva_rate, unit_price_without_tva, total_amount_without_tva)
                    VALUES (@productName, @qty, @price, @total, @saleDate,
                            @tvaRate, @unitPriceWithoutTva, @totalWithoutTva)
                    RETURNING id";

                    using var saleCmd = new NpgsqlCommand(insertSaleQuery, con, tx);
                    saleCmd.Parameters.AddWithValue("@productName", productName);
                    saleCmd.Parameters.AddWithValue("@qty", qty);
                    saleCmd.Parameters.AddWithValue("@price", price);
                    saleCmd.Parameters.AddWithValue("@total", total);
                    saleCmd.Parameters.AddWithValue("@saleDate", saleTime);
                    saleCmd.Parameters.AddWithValue("@tvaRate", tvaRate);
                    saleCmd.Parameters.AddWithValue("@unitPriceWithoutTva", unitPriceWithoutTva);
                    saleCmd.Parameters.AddWithValue("@totalWithoutTva", totalWithoutTva);

                    // الحصول على ID المنشأ
                    var invoiceId = saleCmd.ExecuteScalar();
                    if (invoiceId != null)
                    {
                        generatedInvoiceIds.Add(Convert.ToInt32(invoiceId));
                    }
                }

                tx.Commit();

                // استخدام أول ID منشأ كرقم فاتورة رئيسي
                if (generatedInvoiceIds.Count > 0)
                {
                    currentInvoiceNumber = generatedInvoiceIds[0].ToString();
                }
                else
                {
                    currentInvoiceNumber = DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                // ✅ فتح الدرج النقدي تلقائياً عند اكتمال البيع
                OpenCashDrawer();

                PrintReceiptWithTVA(tvaRate, generatedInvoiceIds.Count > 0 ? generatedInvoiceIds[0] : 0);

                cartTable.Rows.Clear();
                UpdateCartSummary();
                LoadProducts();

                MessageBox.Show("✅ Vente terminée avec succès! Le tiroir-caisse est ouvert.", "Succès");
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                MessageBox.Show($"Erreur lors de la vente: {ex.Message}", "Erreur",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintReceiptWithTVA(decimal tvaRate, int realInvoiceId = 0)
        {
            try
            {
                Form receiptForm = new Form()
                {
                    Text = "FACTURE DE VENTE - TVA",
                    Width = 700,
                    Height = 900,
                    StartPosition = FormStartPosition.CenterScreen,
                    BackColor = Color.White,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false
                };

                int margin = 25;
                int formWidth = receiptForm.ClientSize.Width;

                Panel scrollPanel = new Panel()
                {
                    Left = 0,
                    Top = 0,
                    Width = receiptForm.ClientSize.Width - 5,
                    Height = 760,
                    AutoScroll = true,
                    BackColor = Color.White
                };
                receiptForm.Controls.Add(scrollPanel);

                int y = 20;

                // Titre
                Label title = new Label()
                {
                    Text = "FACTURE DE VENTE",
                    Font = new Font("Arial", 22, FontStyle.Bold),
                    AutoSize = true
                };
                scrollPanel.Controls.Add(title);
                title.Left = (scrollPanel.Width - title.Width) / 2;
                title.Top = y;
                y += title.Height + 10;

                // Store name
                Label store = new Label()
                {
                    Text = "So'Chic",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    AutoSize = true,
                    ForeColor = Color.DarkBlue
                };
                scrollPanel.Controls.Add(store);
                store.Left = (scrollPanel.Width - store.Width) / 2;
                store.Top = y;
                y += store.Height + 20;

                // Numéro de facture - استخدام ID الحقيقي
                string invoiceNumberDisplay = realInvoiceId > 0 ? realInvoiceId.ToString() : currentInvoiceNumber;
                Label invoice = new Label()
                {
                    Text = $"Facture N° : {invoiceNumberDisplay}",
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    AutoSize = true,
                    Left = margin,
                    Top = y
                };
                scrollPanel.Controls.Add(invoice);
                y += invoice.Height + 10;

                // Date
                Label date = new Label()
                {
                    Text = $"Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                    Font = new Font("Arial", 12),
                    AutoSize = true,
                    Left = margin,
                    Top = y
                };
                scrollPanel.Controls.Add(date);
                y += date.Height + 15;

                // TVA
                Label tvaInfo = new Label()
                {
                    Text = $"Taux de TVA appliqué: {tvaRate}%",
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    AutoSize = true,
                    Left = margin,
                    Top = y,
                    ForeColor = Color.Red
                };
                scrollPanel.Controls.Add(tvaInfo);
                y += tvaInfo.Height + 10;

                Panel productPanel = new Panel()
                {
                    Left = margin,
                    Top = y,
                    Width = scrollPanel.Width - margin * 2,
                    Height = 350,
                    AutoScroll = true,
                    BorderStyle = BorderStyle.FixedSingle
                };
                scrollPanel.Controls.Add(productPanel);

                int py = 10;
                decimal totalAmount = 0;
                decimal totalTVA = 0;

                foreach (DataRow row in cartTable.Rows)
                {
                    string name = row["PRODUIT"].ToString();
                    int qty = Convert.ToInt32(row["QUANTITE"]);
                    decimal price = Convert.ToDecimal(row["PRIX"]);
                    decimal total = Convert.ToDecimal(row["TOTAL"]);
                    totalAmount += total;

                    decimal totalWithoutTVA = total / (1 + tvaRate / 100);
                    decimal tvaAmount = total - totalWithoutTVA;
                    totalTVA += tvaAmount;

                    string displayName = name.Length > 25 ? name.Substring(0, 25) + "..." : name;

                    Label lblName = new Label()
                    {
                        Text = displayName,
                        Font = new Font("Arial", 11),
                        AutoSize = false,
                        Width = 200,
                        Left = 5,
                        Top = py
                    };

                    Label lblQty = new Label()
                    {
                        Text = $"x{qty}",
                        Font = new Font("Arial", 11),
                        AutoSize = false,
                        Width = 40,
                        Left = 210,
                        Top = py
                    };

                    Label lblPrice = new Label()
                    {
                        Text = $"{price:N2}",
                        Font = new Font("Arial", 11),
                        AutoSize = false,
                        Width = 80,
                        Left = 260,
                        Top = py
                    };

                    Label lblTotal = new Label()
                    {
                        Text = $"{total:N2}",
                        Font = new Font("Arial", 11, FontStyle.Bold),
                        AutoSize = false,
                        Width = 90,
                        Left = 350,
                        Top = py
                    };

                    productPanel.Controls.Add(lblName);
                    productPanel.Controls.Add(lblQty);
                    productPanel.Controls.Add(lblPrice);
                    productPanel.Controls.Add(lblTotal);

                    py += 25;
                }

                y += productPanel.Height + 20;

                // Détails TVA
                decimal totalHT = totalAmount - totalTVA;

                Label htLabel = new Label()
                {
                    Text = $"Total HT: {totalHT:N2} €",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    AutoSize = true,
                    Left = margin,
                    Top = y
                };
                scrollPanel.Controls.Add(htLabel);
                y += htLabel.Height + 5;

                Label tvaLabel = new Label()
                {
                    Text = $"TVA ({tvaRate}%): {totalTVA:N2} €",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    AutoSize = true,
                    Left = margin,
                    Top = y,
                    ForeColor = Color.Red
                };
                scrollPanel.Controls.Add(tvaLabel);
                y += tvaLabel.Height + 10;

                Label totalLbl = new Label()
                {
                    Text = "TOTAL TTC:",
                    Font = new Font("Arial", 16, FontStyle.Bold),
                    AutoSize = true,
                    Left = margin,
                    Top = y
                };
                scrollPanel.Controls.Add(totalLbl);

                Label totalValue = new Label()
                {
                    Text = $"{totalAmount:N2} €",
                    Font = new Font("Arial", 18, FontStyle.Bold),
                    ForeColor = Color.Green,
                    AutoSize = true,
                    Left = formWidth - margin - 130,
                    Top = y
                };
                scrollPanel.Controls.Add(totalValue);
                y += 70;

                Button closeBtn = new Button()
                {
                    Text = "FERMER",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    Width = 200,
                    Height = 55,
                    Left = (formWidth - 200) / 2,
                    Top = y,
                    BackColor = Color.RoyalBlue,
                    ForeColor = Color.White
                };
                closeBtn.Click += (s, e) => receiptForm.Close();
                scrollPanel.Controls.Add(closeBtn);

                receiptForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur: " + ex.Message);
            }
        }

        // ===================== AFFICHER FACTURES =====================
        private void InvoicesButton_Click(object sender, EventArgs e)
        {
            ShowInvoicesForm();
        }

        private void ShowInvoicesForm()
        {
            Form invoicesForm = new Form()
            {
                Text = "🧾 Liste des Factures ",
                Size = new Size(1200, 800),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            };

            // العنوان الرئيسي
            Label titleLabel = new Label()
            {
                Text = "🧾 LISTE DES FACTURES",
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255),
                Location = new Point(20, 20),
                Size = new Size(800, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // زر حذف الفاتورة (مخفي في البداية)
            Button deleteInvoiceBtn = new Button()
            {
                Text = "🗑️ SUPPRIMER FACTURE",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(250, 50),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Location = new Point(900, 650),
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            deleteInvoiceBtn.FlatAppearance.BorderSize = 0;

            // زر إرجاع الفاتورة
            Button returnInvoiceBtn = new Button()
            {
                Text = "🔄 RETOUR FACTURE",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(250, 50),
                BackColor = Color.FromArgb(255, 165, 0),
                ForeColor = Color.White,
                Location = new Point(900, 580),
                FlatStyle = FlatStyle.Flat
            };
            returnInvoiceBtn.FlatAppearance.BorderSize = 0;

            // مؤقت لإخفاء زر الحذف بعد دقيقتين
            System.Windows.Forms.Timer hideTimer = new System.Windows.Forms.Timer();
            hideTimer.Interval = 120000; // 2 دقيقة
            hideTimer.Tick += (s, e) =>
            {
                deleteInvoiceBtn.Visible = false;
                hideTimer.Stop();
            };

            // معالج حدث الضغط الطويل على العنوان
            DateTime titlePressTime = DateTime.MinValue;
            bool isTitlePressed = false;

            titleLabel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    titlePressTime = DateTime.Now;
                    isTitlePressed = true;
                }
            };

            titleLabel.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && isTitlePressed)
                {
                    isTitlePressed = false;
                    TimeSpan pressDuration = DateTime.Now - titlePressTime;

                    if (pressDuration.TotalSeconds >= 3) // 3 ثواني
                    {
                        deleteInvoiceBtn.Visible = true;
                        deleteInvoiceBtn.BringToFront();
                        hideTimer.Start();
                    }
                }
            };

            DataGridView invoicesGrid = new DataGridView()
            {
                Location = new Point(20, 100),
                Size = new Size(1140, 450),
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                Font = new Font("Arial", 11),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            DataTable invoicesTable = LoadInvoices();
            invoicesGrid.DataSource = invoicesTable;

            if (invoicesGrid.Columns.Count > 0)
            {
                // إضافة عمود ID الفاتورة
                invoicesGrid.Columns["ID_FACTURE"].HeaderText = "ID FACTURE";
                invoicesGrid.Columns["ID_FACTURE"].Width = 100;
                invoicesGrid.Columns["ID_FACTURE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                invoicesGrid.Columns["DATE_FACTURE"].HeaderText = "DATE";
                invoicesGrid.Columns["DATE_FACTURE"].Width = 180;
                invoicesGrid.Columns["DATE_FACTURE"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";

                invoicesGrid.Columns["NB_PRODUITS"].HeaderText = "NB PRODUITS";
                invoicesGrid.Columns["NB_PRODUITS"].Width = 120;
                invoicesGrid.Columns["NB_PRODUITS"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                invoicesGrid.Columns["MONTANT_TOTAL"].HeaderText = "MONTANT TOTAL (€)";
                invoicesGrid.Columns["MONTANT_TOTAL"].Width = 150;
                invoicesGrid.Columns["MONTANT_TOTAL"].DefaultCellStyle.Format = "N2";
                invoicesGrid.Columns["MONTANT_TOTAL"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // حدث زر حذف الفاتورة - بدون أي رسائل
            deleteInvoiceBtn.Click += (s, e) =>
            {
                if (invoicesGrid.SelectedRows.Count == 0)
                {
                    return; // لا شيء يظهر إذا لم يتم تحديد فاتورة
                }

                DataGridViewRow selectedRow = invoicesGrid.SelectedRows[0];
                DateTime invoiceDate = Convert.ToDateTime(selectedRow.Cells["DATE_FACTURE"].Value);

                // حذف الفاتورة مباشرة بدون أي رسائل
                DeleteInvoice(invoiceDate);

                // تحديث الشبكة
                invoicesGrid.DataSource = LoadInvoices();
                deleteInvoiceBtn.Visible = false;
                hideTimer.Stop();
            };

            // حدث زر إرجاع الفاتورة
            returnInvoiceBtn.Click += (s, e) =>
            {
                if (invoicesGrid.SelectedRows.Count == 0)
                {
                    return; // لا شيء يظهر إذا لم يتم تحديد فاتورة
                }

                DataGridViewRow selectedRow = invoicesGrid.SelectedRows[0];
                DateTime invoiceDate = Convert.ToDateTime(selectedRow.Cells["DATE_FACTURE"].Value);

                // إرجاع الفاتورة إلى السلة
                ReturnInvoiceToCart(invoiceDate);

                // تحديث الشبكة
                invoicesGrid.DataSource = LoadInvoices();
            };

            // باقي الأزرار...
            Button viewDetailsBtn = new Button()
            {
                Text = "👁️ Voir Détails",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(200, 50),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                Location = new Point(20, 570),
                FlatStyle = FlatStyle.Flat
            };
            viewDetailsBtn.FlatAppearance.BorderSize = 0;
            viewDetailsBtn.Click += (s, e) => ShowInvoiceDetails(invoicesGrid);

            Button closeBtn = new Button()
            {
                Text = "Fermer",
                Font = new Font("Arial", 14),
                Size = new Size(150, 50),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Location = new Point(240, 570),
                FlatStyle = FlatStyle.Flat
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => invoicesForm.Close();

            // إضافة جميع العناصر إلى النموذج
            invoicesForm.Controls.Add(titleLabel);
            invoicesForm.Controls.Add(deleteInvoiceBtn);
            invoicesForm.Controls.Add(returnInvoiceBtn);
            invoicesForm.Controls.Add(invoicesGrid);
            invoicesForm.Controls.Add(viewDetailsBtn);
            invoicesForm.Controls.Add(closeBtn);

            invoicesForm.ShowDialog();
        }

        // ===================== إرجاع الفاتورة إلى السلة =====================
        private void ReturnInvoiceToCart(DateTime invoiceDate)
        {
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                // الحصول على تفاصيل الفاتورة
                string query = @"SELECT 
                    product_name as PRODUIT,
                    quantity_sold as QUANTITE,
                    unit_price as PRIX_UNITAIRE,
                    total_amount as TOTAL
                FROM sales 
                WHERE sale_date = @saleDate
                AND quantity_sold > 0
                ORDER BY id";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@saleDate", invoiceDate);

                DataTable invoiceDetails = new DataTable();
                using var da = new NpgsqlDataAdapter(cmd);
                da.Fill(invoiceDetails);

                if (invoiceDetails.Rows.Count == 0)
                {
                    return; // لا توجد تفاصيل
                }

                // إضافة المنتجات إلى السلة
                foreach (DataRow row in invoiceDetails.Rows)
                {
                    string productName = row["PRODUIT"].ToString() ?? "";
                    int quantity = Convert.ToInt32(row["QUANTITE"]);
                    decimal unitPrice = Convert.ToDecimal(row["PRIX_UNITAIRE"]);
                    decimal total = Convert.ToDecimal(row["TOTAL"]);

                    // البحث عن ID المنتج
                    int productId = FindProductIdByName(productName);

                    // إضافة إلى السلة مع علامة "RETOUR"
                    cartTable.Rows.Add(productId, "🔄 RETOUR - " + productName, unitPrice, quantity, total);
                }

                UpdateCartSummary();

                // تحديث المخزون (إضافة الكمية المرتجعة)
                UpdateStockForReturn(invoiceDate, con);

                MessageBox.Show($"✅ Facture retournée au panier avec succès! {invoiceDetails.Rows.Count} produits ajoutés.",
                               "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur lors du retour de la facture: {ex.Message}",
                               "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================== تحديث المخزون للإرجاع =====================
        private void UpdateStockForReturn(DateTime invoiceDate, NpgsqlConnection con)
        {
            try
            {
                // تحديث المخزون لكل منتج في الفاتورة
                string updateQuery = @"UPDATE products 
                      SET stock_quantity = stock_quantity + sales.quantity_sold,
                          updated_at = CURRENT_TIMESTAMP
                      FROM sales
                      WHERE LOWER(products.name) = LOWER(sales.product_name)
                      AND sales.sale_date = @saleDate";

                using var updateCmd = new NpgsqlCommand(updateQuery, con);
                updateCmd.Parameters.AddWithValue("@saleDate", invoiceDate);

                int rowsUpdated = updateCmd.ExecuteNonQuery();

                Console.WriteLine($"✅ Stock mis à jour pour {rowsUpdated} produits");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur mise à jour stock: {ex.Message}");
            }
        }

        // ===================== البحث عن ID المنتج بالاسم =====================
        private int FindProductIdByName(string productName)
        {
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                string query = "SELECT id FROM products WHERE LOWER(name) = LOWER(@productName) LIMIT 1";
                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@productName", productName?.Trim() ?? string.Empty);

                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        // ===================== حذف الفاتورة بدون استرداد المخزون =====================
        private void DeleteInvoice(DateTime invoiceDate)
        {
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                // حذف الفاتورة مباشرة بدون استرداد المخزون
                string deleteQuery = "DELETE FROM sales WHERE sale_date = @saleDate";
                using var deleteCmd = new NpgsqlCommand(deleteQuery, con);
                deleteCmd.Parameters.AddWithValue("@saleDate", invoiceDate);
                int deletedRows = deleteCmd.ExecuteNonQuery();

                if (deletedRows > 0)
                {
                    MessageBox.Show($"✅ Facture supprimée avec succès! {deletedRows} enregistrements supprimés.", "Succès");
                }
                else
                {
                    MessageBox.Show("Aucune facture trouvée à supprimer.", "Info");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur lors de la suppression: {ex.Message}", "Erreur");
                Console.WriteLine($"Erreur détaillée: {ex}");
            }
        }

        // ===================== CHARGEMENT FACTURES مع الحذف التلقائي =====================
        private DataTable LoadInvoices()
        {
            DataTable invoicesTable = new DataTable();
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                // أولاً: حذف الفواتير القديمة تلقائياً (أكثر من 15 يوم)
                DeleteOldInvoicesAutomatically(con);

                // ثم تحميل الفواتير المتبقية
                string query = @"SELECT 
            MIN(id) as ID_FACTURE,
            sale_date as DATE_FACTURE,
            COUNT(*) as NB_PRODUITS,
            SUM(total_amount) as MONTANT_TOTAL
        FROM sales 
        WHERE quantity_sold > 0
        GROUP BY sale_date
        ORDER BY sale_date DESC
        LIMIT 500";

                using var da = new NpgsqlDataAdapter(query, con);
                invoicesTable.Clear();
                da.Fill(invoicesTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement factures: {ex.Message}", "Erreur");
            }
            return invoicesTable;
        }

        // ===================== الحذف التلقائي للفواتير القديمة =====================
        private void DeleteOldInvoicesAutomatically(NpgsqlConnection con)
        {
            try
            {
                // حساب التاريخ قبل 15 يوم
                DateTime deleteBeforeDate = DateTime.Now.AddDays(-15);

                string deleteOldQuery = @"DELETE FROM sales 
                                WHERE sale_date < @deleteBeforeDate";

                using var deleteCmd = new NpgsqlCommand(deleteOldQuery, con);
                deleteCmd.Parameters.AddWithValue("@deleteBeforeDate", deleteBeforeDate);

                int deletedCount = deleteCmd.ExecuteNonQuery();

                if (deletedCount > 0)
                {
                    Console.WriteLine($"✅ Suppression automatique: {deletedCount} factures anciennes supprimées");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur suppression automatique: {ex.Message}");
            }
        }

        // ===================== AFFICHER DÉTAILS FACTURE =====================
        private void ShowInvoiceDetails(DataGridView invoicesGrid)
        {
            if (invoicesGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner une facture.", "Info");
                return;
            }

            DataGridViewRow selectedRow = invoicesGrid.SelectedRows[0];
            DateTime saleDate = Convert.ToDateTime(selectedRow.Cells["DATE_FACTURE"].Value);
            int invoiceId = Convert.ToInt32(selectedRow.Cells["ID_FACTURE"].Value);

            Form detailsForm = new Form()
            {
                Text = $"🧾 Détails Facture - {saleDate:dd/MM/yyyy HH:mm:ss}",
                Size = new Size(1000, 700),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            };

            Label titleLabel = new Label()
            {
                Text = $"DÉTAILS FACTURE - ID: {invoiceId}",
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            DataGridView detailsGrid = new DataGridView()
            {
                Location = new Point(20, 80),
                Size = new Size(940, 450),
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                Font = new Font("Arial", 11),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            DataTable detailsTable = LoadInvoiceDetails(saleDate);
            detailsGrid.DataSource = detailsTable;

            if (detailsGrid.Columns.Count > 0)
            {
                // إظهار ID الفاتورة في التفاصيل
                detailsGrid.Columns["ID_FACTURE"].HeaderText = "ID FACTURE";
                detailsGrid.Columns["ID_FACTURE"].Width = 100;
                detailsGrid.Columns["ID_FACTURE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                detailsGrid.Columns["PRODUIT"].HeaderText = "PRODUIT";
                detailsGrid.Columns["PRODUIT"].Width = 250;

                detailsGrid.Columns["QUANTITE"].HeaderText = "QUANTITÉ";
                detailsGrid.Columns["QUANTITE"].Width = 80;
                detailsGrid.Columns["QUANTITE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                detailsGrid.Columns["PRIX_UNITAIRE"].HeaderText = "PRIX UNITAIRE (€)";
                detailsGrid.Columns["PRIX_UNITAIRE"].Width = 120;
                detailsGrid.Columns["PRIX_UNITAIRE"].DefaultCellStyle.Format = "N2";
                detailsGrid.Columns["PRIX_UNITAIRE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                detailsGrid.Columns["TOTAL"].HeaderText = "TOTAL (€)";
                detailsGrid.Columns["TOTAL"].Width = 120;
                detailsGrid.Columns["TOTAL"].DefaultCellStyle.Format = "N2";
                detailsGrid.Columns["TOTAL"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                // إضافة أعمدة TVA إذا كانت موجودة
                if (detailsGrid.Columns.Contains("TVA"))
                {
                    detailsGrid.Columns["TVA"].HeaderText = "TVA (%)";
                    detailsGrid.Columns["TVA"].Width = 80;
                    detailsGrid.Columns["TVA"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (detailsGrid.Columns.Contains("PRIX_HT"))
                {
                    detailsGrid.Columns["PRIX_HT"].HeaderText = "PRIX HT (€)";
                    detailsGrid.Columns["PRIX_HT"].Width = 120;
                    detailsGrid.Columns["PRIX_HT"].DefaultCellStyle.Format = "N2";
                    detailsGrid.Columns["PRIX_HT"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            decimal totalAmount = 0;
            foreach (DataRow row in detailsTable.Rows)
            {
                totalAmount += Convert.ToDecimal(row["TOTAL"]);
            }

            Label totalLabel = new Label()
            {
                Text = $"MONTANT TOTAL: {totalAmount:N2} €",
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.Green,
                AutoSize = true,
                Location = new Point(20, 550)
            };

            Button closeBtn = new Button()
            {
                Text = "Fermer",
                Font = new Font("Arial", 14),
                Size = new Size(150, 45),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Location = new Point(20, 600),
                FlatStyle = FlatStyle.Flat
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => detailsForm.Close();

            detailsForm.Controls.Add(titleLabel);
            detailsForm.Controls.Add(detailsGrid);
            detailsForm.Controls.Add(totalLabel);
            detailsForm.Controls.Add(closeBtn);

            detailsForm.ShowDialog();
        }

        // ===================== CHARGEMENT DÉTAILS FACTURE =====================
        private DataTable LoadInvoiceDetails(DateTime saleDate)
        {
            DataTable detailsTable = new DataTable();
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                string query = @"SELECT 
            id as ID_FACTURE,
            product_name as PRODUIT,
            quantity_sold as QUANTITE,
            unit_price as PRIX_UNITAIRE,
            total_amount as TOTAL,
            tva_rate as TVA,
            unit_price_without_tva as PRIX_HT,
            total_amount_without_tva as TOTAL_HT
        FROM sales 
        WHERE sale_date = @saleDate
        AND quantity_sold > 0
        ORDER BY id";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@saleDate", saleDate);

                using var da = new NpgsqlDataAdapter(cmd);
                detailsTable.Clear();
                da.Fill(detailsTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement détails: {ex.Message}", "Erreur");
            }
            return detailsTable;
        }

        // ===================== ACTUALISER TOUS LES FORMULAIRES DE RAPPORT =====================
        private void RefreshAllRapportForms()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form is RapportForm rapportForm)
                {
                    rapportForm.RefreshReport();
                }
            }
        }
        // ===================== OUVRIR TIROIR-CAISSE =====================
        private void OpenCashDrawer()
        {
            try
            {
                bool drawerOpened = false;

                // المحاولة الأولى: استخدام الأمر المباشر للطابعة
                string printerName = GetDefaultPrinter();
                if (!string.IsNullOrEmpty(printerName))
                {
                    try
                    {
                        using (var printDoc = new PrintDocument())
                        {
                            printDoc.PrinterSettings.PrinterName = printerName;
                            if (printDoc.PrinterSettings.IsValid)
                            {
                                // أمر فتح الدرج لمعظم الطابعات
                                string openDrawerCommand = "\x1B\x70\x00\x19\xFA";

                                printDoc.PrintPage += (sender, e) =>
                                {
                                    e.Graphics?.DrawString(openDrawerCommand,
                                        new Font("Arial", 1), Brushes.Black, 0, 0);
                                };

                                printDoc.Print();
                                drawerOpened = true;
                                Console.WriteLine($"✅ Tiroir ouvert avec succès sur l'imprimante: {printerName}");
                            }
                        }
                    }
                    catch (Exception printerEx)
                    {
                        Console.WriteLine($"❌ Erreur imprimante: {printerEx.Message}");
                    }
                }

                // المحاولة الثانية: استخدام المنفذ التسلسلي
                if (!drawerOpened)
                {
                    TryOpenSerialCashDrawer();
                }

                if (!drawerOpened)
                {
                    MessageBox.Show("❌ Impossible d'ouvrir le tiroir-caisse automatiquement.\nVeuillez l'ouvrir manuellement.",
                                  "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur ouverture tiroir-caisse: {ex.Message}");
                MessageBox.Show($"Erreur ouverture tiroir: {ex.Message}", "Erreur");
            }
        }

        // ===================== OUVRIR TIROIR VIA PORT SÉRIE =====================
        private void TryOpenSerialCashDrawer()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    try
                    {
                        using (SerialPort serialPort = new SerialPort(port, 9600, Parity.None, 8, StopBits.One))
                        {
                            serialPort.Open();
                            byte[] openCommand = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };
                            serialPort.Write(openCommand, 0, openCommand.Length);
                            serialPort.Close();
                            Console.WriteLine($"✅ Tiroir ouvert sur le port {port}");
                            return;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur tiroir série: {ex.Message}");
            }
        }

        // ===================== CLASS RAPPORTFORM (إن وجد) =====================
        public class RapportForm : Form
        {
            public void RefreshReport()
            {
                // طريقة لتحديث التقارير
                // يمكنك تنفيذ هذا حسب احتياجاتك
            }
        }

        
        // ===================== OBTENIR IMPRIMANTE PAR DÉFAUT =====================
        private string GetDefaultPrinter()
        {
            try
            {
                var settings = new PrinterSettings();
                return settings.PrinterName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur obtention imprimante: {ex.Message}");
                return null;
            }
        }
        // ÉVÉNEMENTS
        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            SearchProducts(searchBox.Text.Trim());
        }

        private void ProductsGrid_DoubleClick(object sender, EventArgs e) => AddToCart();
        private void AddButton_Click(object sender, EventArgs e) => AddToCart();
        private void RemoveButton_Click(object sender, EventArgs e) => RemoveFromCart();
        private void ClearButton_Click(object sender, EventArgs e) => ClearCart();

        private void ProductsGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            FormatDataGrids();
        }


    }

}