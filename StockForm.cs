using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace ClothingStoreApp
{
    public class StockForm : Form
    {
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=clothing_store;Username=postgres;Password=0000;";

        private DataGridView dgv = new DataGridView();
        private TextBox searchBox = new TextBox();
        private Button addBtn = new Button();
        private Button editBtn = new Button();
        private Button deleteBtn = new Button();
        private Button backBtn = new Button();
        private Panel titleBar = new Panel();
        private Panel buttonsPanel = new Panel();
        private Panel gridPanel = new Panel();

        public StockForm()
        {
            InitializeComponents();
            LoadProducts();
        }

        private void InitializeComponents()
        {
            SetupForm();
            CreateTitleBar();
            CreateButtonsPanel();
            CreateGridPanel();
            PositionControls();
        }

        private void SetupForm()
        {
            this.Text = "Gestion du Stock";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
        }

        private void CreateTitleBar()
        {
            titleBar.BackColor = Color.FromArgb(25, 25, 25);
            titleBar.Height = 100;
            titleBar.Width = this.ClientSize.Width;

            // زر العودة
            backBtn.Text = "←";
            backBtn.Font = new Font("Arial", 26, FontStyle.Bold);
            backBtn.Size = new Size(80, 80);
            backBtn.BackColor = Color.FromArgb(74, 144, 226);
            backBtn.FlatStyle = FlatStyle.Flat;
            backBtn.ForeColor = Color.White;
            backBtn.Location = new Point(20, 10);
            backBtn.FlatAppearance.BorderSize = 0;
            backBtn.Click += (s, e) => this.Close();

            // العنوان
            Label titleLabel = new Label
            {
                Text = "GESTION DU STOCK",
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(120, 35)
            };

            // شريط البحث
            searchBox.Font = new Font("Arial", 16);
            searchBox.Width = 400;
            searchBox.Height = 40;
            searchBox.Location = new Point((titleBar.Width - 400) / 2, 30);
            searchBox.PlaceholderText = "Rechercher par nom, catégorie, code-barres...";
            searchBox.TextChanged += (s, e) => SearchProducts(searchBox.Text);

            // زر الإغلاق
            Button closeBtn = new Button
            {
                Text = "X",
                Font = new Font("Arial", 26, FontStyle.Bold),
                Size = new Size(80, 80),
                BackColor = Color.Red,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Location = new Point(titleBar.Width - 90, 10)
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => Application.Exit();

            // زر التصغير
            Button minimizeBtn = new Button
            {
                Text = "_",
                Font = new Font("Arial", 26, FontStyle.Bold),
                Size = new Size(80, 80),
                BackColor = Color.Gray,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Location = new Point(titleBar.Width - 170, 10)
            };
            minimizeBtn.FlatAppearance.BorderSize = 0;
            minimizeBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            titleBar.Controls.Add(backBtn);
            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(searchBox);
            titleBar.Controls.Add(closeBtn);
            titleBar.Controls.Add(minimizeBtn);

            this.Controls.Add(titleBar);
        }

        private void CreateButtonsPanel()
        {
            buttonsPanel.BackColor = Color.FromArgb(240, 240, 240);
            buttonsPanel.Height = 70;
            buttonsPanel.Width = this.ClientSize.Width;

            addBtn = CreateButton("AJOUTER", Color.FromArgb(46, 204, 113));
            editBtn = CreateButton("MODIFIER", Color.FromArgb(52, 152, 219));
            deleteBtn = CreateButton("SUPPRIMER", Color.FromArgb(231, 76, 60));

            addBtn.Click += AddBtn_Click;
            editBtn.Click += EditBtn_Click;
            deleteBtn.Click += DeleteBtn_Click;

            buttonsPanel.Controls.Add(addBtn);
            buttonsPanel.Controls.Add(editBtn);
            buttonsPanel.Controls.Add(deleteBtn);

            this.Controls.Add(buttonsPanel);
        }

        private void CreateGridPanel()
        {
            gridPanel.BackColor = Color.White;
            gridPanel.BorderStyle = BorderStyle.FixedSingle;

            dgv.Font = new Font("Arial", 10);
            dgv.RowTemplate.Height = 35;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.BackgroundColor = Color.White;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // تغيير إلى None للتحكم اليدوي
            dgv.BorderStyle = BorderStyle.None;
            dgv.AllowUserToResizeRows = false;
            dgv.ColumnHeadersHeight = 40;
            dgv.RowHeadersVisible = false;

            dgv.DataError += Dgv_DataError;

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.Black,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.DefaultCellStyle.Padding = new Padding(3);

            gridPanel.Controls.Add(dgv);
            this.Controls.Add(gridPanel);
        }

        private void Dgv_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        private void PositionControls()
        {
            titleBar.Location = new Point(0, 0);
            titleBar.Size = new Size(this.ClientSize.Width, 100);

            buttonsPanel.Location = new Point(0, 100);
            buttonsPanel.Size = new Size(this.ClientSize.Width, 70);

            // الجدول على كامل الصفحة
            gridPanel.Location = new Point(0, 170);
            gridPanel.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 170);

            dgv.Location = new Point(0, 0);
            dgv.Size = new Size(gridPanel.Width, gridPanel.Height);

            PositionButtons();
            UpdateTitleBarButtons();
        }

        private void UpdateTitleBarButtons()
        {
            foreach (Control control in titleBar.Controls)
            {
                if (control is Button btn)
                {
                    if (btn.Text == "X")
                    {
                        btn.Location = new Point(titleBar.Width - 90, 10);
                    }
                    else if (btn.Text == "_")
                    {
                        btn.Location = new Point(titleBar.Width - 170, 10);
                    }
                }
                else if (control is TextBox)
                {
                    control.Location = new Point((titleBar.Width - control.Width) / 2, 30);
                }
            }
        }

        private Button CreateButton(string text, Color color)
        {
            return new Button
            {
                Text = text,
                Font = new Font("Arial", 11, FontStyle.Bold),
                Size = new Size(180, 45),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        private void PositionButtons()
        {
            if (buttonsPanel == null) return;

            int buttonWidth = 180;
            int spacing = 20;
            int totalWidth = (buttonWidth * 3) + (spacing * 2);
            int startX = (buttonsPanel.Width - totalWidth) / 2;

            if (addBtn != null) addBtn.Location = new Point(startX, 12);
            if (editBtn != null) editBtn.Location = new Point(startX + buttonWidth + spacing, 12);
            if (deleteBtn != null) deleteBtn.Location = new Point(startX + (buttonWidth + spacing) * 2, 12);
        }

        private void LoadProducts()
        {
            try
            {
                if (dgv == null) return;

                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                string query = @"SELECT 
                                    name as ""PRODUIT"",
                                    price as ""PRIX"",
                                    stock_quantity as ""QUANTITE"",
                                    size as ""TAILLE"",
                                    color as ""COULEUR"",
                                    category as ""CATEGORIE"",
                                    barcode as ""CODE BARRE"",
                                    created_at as ""DATE AJOUT"",
                                    updated_at as ""DATE MODIF"",
                                    id as ""ID""  -- نحتفظ به داخلياً ولكن لن نعرضه
                                FROM products 
                                ORDER BY id DESC";

                using var da = new NpgsqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dgv.DataSource = dt;
                FormatDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de base de données: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatDataGridView()
        {
            if (dgv == null || dgv.Columns.Count == 0) return;

            try
            {
                // إخفاء عمود ID
                if (dgv.Columns.Contains("ID"))
                {
                    dgv.Columns["ID"].Visible = false;
                }

                // حساب العرض الإجمالي للجدول
                int totalWidth = dgv.Width - 20; // ناقص 20 بكسل للهامش

                // توزيع الأعمدة مع إعطاء الأولوية لعمود PRODUIT
                if (dgv.Columns.Contains("PRODUIT"))
                {
                    dgv.Columns["PRODUIT"].Width = (int)(totalWidth * 0.25); // 25% من العرض
                }

                if (dgv.Columns.Contains("PRIX"))
                {
                    dgv.Columns["PRIX"].Width = (int)(totalWidth * 0.08); // 8% من العرض
                    dgv.Columns["PRIX"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgv.Columns["PRIX"].DefaultCellStyle.Format = "N2";
                }

                if (dgv.Columns.Contains("QUANTITE"))
                {
                    dgv.Columns["QUANTITE"].Width = (int)(totalWidth * 0.07); // 7% من العرض
                    dgv.Columns["QUANTITE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (dgv.Columns.Contains("TAILLE"))
                {
                    dgv.Columns["TAILLE"].Width = (int)(totalWidth * 0.06); // 6% من العرض
                    dgv.Columns["TAILLE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (dgv.Columns.Contains("COULEUR"))
                {
                    dgv.Columns["COULEUR"].Width = (int)(totalWidth * 0.08); // 8% من العرض
                    dgv.Columns["COULEUR"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (dgv.Columns.Contains("CATEGORIE"))
                {
                    dgv.Columns["CATEGORIE"].Width = (int)(totalWidth * 0.12); // 12% من العرض
                }

                if (dgv.Columns.Contains("CODE BARRE"))
                {
                    dgv.Columns["CODE BARRE"].Width = (int)(totalWidth * 0.12); // 12% من العرض
                    dgv.Columns["CODE BARRE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (dgv.Columns.Contains("DATE AJOUT"))
                {
                    dgv.Columns["DATE AJOUT"].Width = (int)(totalWidth * 0.11); // 11% من العرض
                    dgv.Columns["DATE AJOUT"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgv.Columns["DATE AJOUT"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                }

                if (dgv.Columns.Contains("DATE MODIF"))
                {
                    dgv.Columns["DATE MODIF"].Width = (int)(totalWidth * 0.11); // 11% من العرض
                    dgv.Columns["DATE MODIF"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgv.Columns["DATE MODIF"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                }

                // تلوين الصفوف حسب الكمية
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;

                    if (row.Cells["QUANTITE"].Value != null)
                    {
                        if (row.Cells["QUANTITE"].Value is int quantity)
                        {
                            ApplyRowColor(row, quantity);
                        }
                        else if (int.TryParse(row.Cells["QUANTITE"].Value.ToString(), out int qty))
                        {
                            ApplyRowColor(row, qty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Format error: {ex.Message}");
            }
        }

        private void ApplyRowColor(DataGridViewRow row, int quantity)
        {
            if (quantity == 0)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(200, 0, 0);
            }
            else if (quantity <= 5)
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

        private void SearchProducts(string keyword)
        {
            try
            {
                if (dgv == null) return;

                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                string query =
                    @"SELECT 
                        name as ""PRODUIT"", 
                        price as ""PRIX"",
                        stock_quantity as ""QUANTITE"",
                        size as ""TAILLE"",
                        color as ""COULEUR"",
                        category as ""CATEGORIE"",
                        barcode as ""CODE BARRE"",
                        created_at as ""DATE AJOUT"",
                        updated_at as ""DATE MODIF"",
                        id as ""ID""  -- نحتفظ به داخلياً ولكن لن نعرضه
                      FROM products
                      WHERE LOWER(name) LIKE LOWER(@kw) 
                         OR LOWER(category) LIKE LOWER(@kw)
                         OR LOWER(color) LIKE LOWER(@kw)
                         OR LOWER(size) LIKE LOWER(@kw)
                         OR LOWER(barcode) LIKE LOWER(@kw)
                         OR CAST(price AS TEXT) LIKE @kw
                      ORDER BY id DESC";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                using var da = new NpgsqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dgv.DataSource = dt;
                FormatDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de recherche: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddBtn_Click(object sender, EventArgs e)
        {
            try
            {
                AddProductForm addForm = new AddProductForm();
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgv == null || dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Veuillez sélectionner un produit à modifier.", "Information");
                    return;
                }

                // الحصول على ID من العمود المخفي
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);
                EditProductForm editForm = new EditProductForm(id);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgv == null || dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Veuillez sélectionner un produit à supprimer.", "Information");
                    return;
                }

                // الحصول على ID من العمود المخفي
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);
                string productName = dgv.SelectedRows[0].Cells["PRODUIT"].Value?.ToString() ?? "";

                if (MessageBox.Show($"Voulez-vous vraiment supprimer le produit '{productName}' ?",
                    "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    using var con = new NpgsqlConnection(connectionString);
                    con.Open();

                    using var cmd = new NpgsqlCommand("DELETE FROM products WHERE id = @id", con);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();

                    LoadProducts();
                    MessageBox.Show("Produit supprimé avec succès!", "Succès");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PositionControls();
            // إعادة تنسيق الجدول عند تغيير حجم النافذة
            if (dgv != null && dgv.Columns.Count > 0)
            {
                FormatDataGridView();
            }
        }
    }
}