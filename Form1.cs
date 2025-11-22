using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace ClothingStoreApp
{
    public partial class Form1 : Form
    {
        private Panel? titleBar;
        private Panel? mainGrid;
        private System.Windows.Forms.Timer? timer;

        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=clothing_store;Username=postgres;Password=0000;";

        public Form1()
        {
            InitializeComponent();
            SetupForm();
            CreateTitleBar();
            CreateMainTiles();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(1280, 800);
            this.Name = "Form1";
            this.Text = "Clothing Store App";
            this.Load += Form1_Load;
            this.ResumeLayout(false);
        }

        private void SetupForm()
        {
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.White;
        }

        //══════════════════════════════════════════
        // 1) BARRE DE TITRE
        //══════════════════════════════════════════
        private void CreateTitleBar()
        {
            titleBar = new Panel
            {
                BackColor = Color.FromArgb(25, 25, 25),
                Dock = DockStyle.Top,
                Height = 100
            };

            // Bouton Fermer
            var closeBtn = new Button
            {
                Text = "✕",
                Font = new Font("Arial", 26, FontStyle.Bold),
                Size = new Size(80, 80),
                BackColor = Color.Red,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (_, _) => Application.Exit();

            // Bouton Réduire
            var minimizeBtn = new Button
            {
                Text = "—",
                Font = new Font("Arial", 26, FontStyle.Bold),
                Size = new Size(80, 80),
                BackColor = Color.Gray,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            minimizeBtn.FlatAppearance.BorderSize = 0;
            minimizeBtn.Click += (_, _) => this.WindowState = FormWindowState.Minimized;

            // Horloge
            Label clockLabel = new Label
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Font = new Font("Arial", 36, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            // Notifications
            int notifCount = CheckLowStockNotifications();
            var notifBtn = new Button
            {
                Text = $"🔔 {notifCount}",
                Font = new Font("Arial", 24, FontStyle.Bold),
                Size = new Size(130, 80),
                BackColor = notifCount > 0 ? Color.DarkRed : Color.DimGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            notifBtn.FlatAppearance.BorderSize = 0;
            notifBtn.Click += NotificationBtn_Click;

            titleBar.Controls.Add(closeBtn);
            titleBar.Controls.Add(minimizeBtn);
            titleBar.Controls.Add(clockLabel);
            titleBar.Controls.Add(notifBtn);

            titleBar.Resize += (_, _) =>
            {
                closeBtn.Location = new Point(titleBar.Width - 80, 10);
                minimizeBtn.Location = new Point(titleBar.Width - 170, 10);
                clockLabel.Location = new Point((titleBar.Width - clockLabel.Width) / 2, 25);
                notifBtn.Location = new Point(20, 10);
            };

            this.Controls.Add(titleBar);
            MakeDraggable(titleBar);
        }

        private void MakeDraggable(Control ctrl)
        {
            bool dragging = false;
            Point start = Point.Empty;

            ctrl.MouseDown += (s, e) =>
            {
                dragging = true;
                start = e.Location;
            };

            ctrl.MouseMove += (s, e) =>
            {
                if (dragging)
                {
                    this.Left += e.X - start.X;
                    this.Top += e.Y - start.Y;
                }
            };

            ctrl.MouseUp += (_, _) => dragging = false;
        }

        //══════════════════════════════════════════
        // 2) TUILES PRINCIPALES
        //══════════════════════════════════════════
        private void CreateMainTiles()
        {
            mainGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            string[] titles =
            {
                "🛒 Ventes",
                "📦 Stock",
                "➕ Ajouter Produit",
                "📊 Rapports",
            };

            Color[] colors =
            {
                Color.FromArgb(65, 179, 163),
                Color.FromArgb(74, 144, 226),
                Color.FromArgb(165, 94, 234),
                Color.FromArgb(237, 137, 54),
            };

            for (int i = 0; i < titles.Length; i++)
                CreateTile(titles[i], colors[i], i);

            this.Controls.Add(mainGrid);
        }

        private void CreateTile(string title, Color color, int index)
        {
            Panel tile = new Panel
            {
                Size = new Size(350, 280),
                BackColor = color,
                Cursor = Cursors.Hand,
                Name = $"tile_{title.Replace(" ", "")}"
            };

            // تأثيرات التمرير
            tile.MouseEnter += (s, e) =>
            {
                tile.BackColor = ControlPaint.Light(color, 0.3f);
                tile.Size = new Size(355, 285);
                tile.Location = new Point(tile.Location.X - 2, tile.Location.Y - 2);
            };

            tile.MouseLeave += (s, e) =>
            {
                tile.BackColor = color;
                tile.Size = new Size(350, 280);
                tile.Location = new Point(tile.Location.X + 2, tile.Location.Y + 2);
            };

            // حدث النقر
            tile.Click += (s, e) =>
            {
                TileClicked(title);
            };

            Label lbl = new Label
            {
                Text = title,
                Font = new Font("Arial", 26, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            lbl.Click += (s, e) =>
            {
                TileClicked(title);
            };

            tile.Controls.Add(lbl);

            int tilesPerRow = 2;
            int row = index / tilesPerRow;
            int col = index % tilesPerRow;

            int horizontalSpacing = 100;
            int verticalSpacing = 80;
            int startX = (this.ClientSize.Width - (tilesPerRow * 350 + (tilesPerRow - 1) * horizontalSpacing)) / 2 + 300;
            int startY = 200;

            tile.Location = new Point(
                startX + col * (350 + horizontalSpacing),
                startY + row * (280 + verticalSpacing)
            );

            mainGrid.Controls.Add(tile);
        }

        private void TileClicked(string tileName)
        {
            switch (tileName)
            {
                case "🛒 Ventes":
                    ShowSalesForm();
                    break;
                case "📦 Stock":
                    ShowStockForm();
                    break;
                case "➕ Ajouter Produit":
                    ShowAddProductForm();
                    break;
                case "📊 Rapports":
                    ShowRapportForm();
                    break;
                default:
                    MessageBox.Show($"Tile inconnue: {tileName}", "Erreur");
                    break;
            }
        }

        //══════════════════════════════════════════
        // 3) NOTIFICATIONS
        //══════════════════════════════════════════
        private void NotificationBtn_Click(object? sender, EventArgs e)
        {
            ShowNotificationsMenu(sender);
        }

        private void ShowNotificationsMenu(object? sender)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = new Font("Arial", 12);

            int notificationCount = CheckLowStockNotifications();
            DataTable lowStockProducts = GetLowStockProducts();

            ToolStripMenuItem headerItem = new ToolStripMenuItem($"🔔 Notifications ({notificationCount})");
            headerItem.Font = new Font("Arial", 14, FontStyle.Bold);
            headerItem.Enabled = false;
            menu.Items.Add(headerItem);
            menu.Items.Add(new ToolStripSeparator());

            if (notificationCount > 0)
            {
                foreach (DataRow row in lowStockProducts.Rows)
                {
                    string productName = row["name"]?.ToString() ?? "";
                    int stock = row["stock_quantity"] != DBNull.Value ? Convert.ToInt32(row["stock_quantity"]) : 0;
                    string size = row["size"]?.ToString() ?? "";
                    string color = row["color"]?.ToString() ?? "";
                    string category = row["category"]?.ToString() ?? "";

                    string displayText = $"📦 {productName}";
                    if (!string.IsNullOrEmpty(size)) displayText += $" - {size}";
                    if (!string.IsNullOrEmpty(color)) displayText += $" ({color})";
                    if (!string.IsNullOrEmpty(category)) displayText += $" [{category}]";
                    displayText += $" - {stock} restant(s)";

                    ToolStripMenuItem alertItem = new ToolStripMenuItem(displayText);
                    alertItem.Tag = productName;
                    alertItem.Click += (s, e) => ShowNotificationDetails(productName, stock, size, color, category);
                    alertItem.Font = new Font("Arial", 11);
                    menu.Items.Add(alertItem);
                }
            }
            else
            {
                ToolStripMenuItem noAlertsItem = new ToolStripMenuItem("✅ Aucune alerte de stock");
                noAlertsItem.Enabled = false;
                noAlertsItem.Font = new Font("Arial", 11);
                menu.Items.Add(noAlertsItem);
            }

            if (sender is Button btn)
            {
                menu.Show(btn, new Point(0, btn.Height));
            }
        }

        private void ShowNotificationDetails(string productName, int stock, string size, string color, string category)
        {
            Form detailsForm = new Form
            {
                Text = "Détails de l'Alerte de Stock",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30)
            };

            Label titleLabel = new Label
            {
                Text = "📦 Détails du Produit en Stock Faible",
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Panel detailsPanel = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250),
                Size = new Size(500, 180),
                Location = new Point(20, 70)
            };

            Label detailsLabel = new Label
            {
                Text = $"🔸 Nom: {productName}\n" +
                       $"🔸 Stock restant: {stock} unités\n" +
                       $"🔸 Taille: {(string.IsNullOrEmpty(size) ? "Non spécifié" : size)}\n" +
                       $"🔸 Couleur: {(string.IsNullOrEmpty(color) ? "Non spécifié" : color)}\n" +
                       $"🔸 Catégorie: {(string.IsNullOrEmpty(category) ? "Non spécifié" : category)}",
                Font = new Font("Arial", 14),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Location = new Point(15, 15)
            };

            detailsPanel.Controls.Add(detailsLabel);

            Button deleteBtn = new Button
            {
                Text = "🗑️ Supprimer l'alerte",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Size = new Size(180, 45),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Location = new Point(100, 280),
                FlatStyle = FlatStyle.Flat
            };
            deleteBtn.FlatAppearance.BorderSize = 0;
            deleteBtn.Click += (s, e) =>
            {
                if (MessageBox.Show($"Voulez-vous vraiment supprimer l'alerte pour {productName}?",
                    "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    RemoveNotification(productName);
                    detailsForm.Close();
                }
            };

            Button closeBtn = new Button
            {
                Text = "Fermer",
                Font = new Font("Arial", 12),
                Size = new Size(120, 45),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Location = new Point(320, 280),
                FlatStyle = FlatStyle.Flat
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => detailsForm.Close();

            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(detailsPanel);
            mainPanel.Controls.Add(deleteBtn);
            mainPanel.Controls.Add(closeBtn);
            detailsForm.Controls.Add(mainPanel);

            detailsForm.ShowDialog();
        }

        private void RemoveNotification(string productName)
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"UPDATE products SET stock_quantity = 10 
                                   WHERE name = @ProductName";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProductName", productName);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Alerte supprimée avec succès!", "Succès",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            UpdateNotifications();
                        }
                        else
                        {
                            MessageBox.Show("Produit non trouvé!", "Erreur",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int CheckLowStockNotifications()
        {
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM products WHERE stock_quantity <= 5", con);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private DataTable GetLowStockProducts()
        {
            DataTable dt = new DataTable();
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT name, stock_quantity, size, color, category 
                                   FROM products WHERE stock_quantity <= 5 
                                   ORDER BY stock_quantity ASC";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de base de données: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return dt;
        }

        //══════════════════════════════════════════
        // 4) NAVIGATION
        //══════════════════════════════════════════
        private void ShowSalesForm()
        {
            try
            {
                VentesForm VentesFormM = new VentesForm();
                OpenFormAndHide(VentesFormM);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture des ventes: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowRapportForm()
        {
            try
            {
                RapportForm ShowRapportFormM = new RapportForm();
                OpenFormAndHide(ShowRapportFormM);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture des ventes: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowStockForm()
        {
            try
            {
                StockForm stockForm = new StockForm();
                OpenFormAndHide(stockForm);
                stockForm.FormClosed += (s, e) => UpdateNotifications();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de Stock: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAddProductForm()
        {
            try
            {
                AddProductForm addForm = new AddProductForm();
                addForm.Show();

                addForm.FormClosed += (s, e) => UpdateNotifications();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateNotifications()
        {
            int notifCount = CheckLowStockNotifications();
            if (titleBar != null)
            {
                foreach (Control ctrl in titleBar.Controls)
                {
                    if (ctrl is Button btn && btn.Text.StartsWith("🔔"))
                    {
                        btn.Text = $"🔔 {notifCount}";
                        btn.BackColor = notifCount > 0 ? Color.DarkRed : Color.DimGray;
                        break;
                    }
                }
            }
        }

        //══════════════════════════════════════════
        // 5) MISE À JOUR AUTOMATIQUE DE L'HORLOGE
        //══════════════════════════════════════════
        private void Form1_Load(object? sender, EventArgs e)
        {
            timer = new System.Windows.Forms.Timer { Interval = 60000 };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (titleBar != null)
            {
                foreach (Control ctrl in titleBar.Controls)
                {
                    if (ctrl is Label lbl && lbl.Font.Size > 30)
                        lbl.Text = DateTime.Now.ToString("HH:mm");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer?.Stop();
                timer?.Dispose();
            }
            base.Dispose(disposing);
        }

        //══════════════════════════════════════════
// 6) إدارة فتح وإغلاق النماذج
//══════════════════════════════════════════
private void OpenFormAndHide(Form form)
        {
            this.Hide(); // إخفاء Form1
            form.FormClosed += (s, args) =>
            {
                this.Show(); // إعادة إظهار Form1 عند إغلاق النموذج
                this.BringToFront(); // جعله في المقدمة
            };
            form.Show();
        }
    }
}