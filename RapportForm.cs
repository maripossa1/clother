using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using System.Linq;

namespace ClothingStoreApp
{
    public class RapportForm : Form
    {
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=clothing_store;Username=postgres;Password=0000;";

        private Panel titleBar;
        private Button closeBtn, minimizeBtn, backBtn;
        private Label titleLabel;

        private DataGridView dailyGrid;
        private DateTimePicker selectedDate;
        // Nouvelle étiquette pour le nombre de factures
        private Label todayRevenueLabel, todaySalesLabel, todayReturnsLabel, todayInvoicesLabel;

        private System.Windows.Forms.Timer refreshTimer;

        public RapportForm()
        {
            InitializeForm();
            CreateTitleBar();
            CreateLayout();

            this.Load += (s, e) => LoadDailyData();

            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000;
            refreshTimer.Tick += (s, e) => LoadDailyData();
            refreshTimer.Start();

            EnableSecretClickOnTitle();
        }

        private void InitializeForm()
        {
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;
            this.Text = "Rapport du Jour";
        }

        private void CreateTitleBar()
        {
            // Code existant pour CreateTitleBar...
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
                Text = "💰 RAPPORT QUOTIDIEN",
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Cursor = Cursors.Hand
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
            // Fin du code existant pour CreateTitleBar...
        }

        private void CreateLayout()
        {
            if (titleBar == null) return;

            int startY = titleBar.Height + 30;

            // ========== Upper Section (Date + Cards) ==========
            Panel topContainer = new Panel()
            {
                Location = new Point(20, startY),
                Size = new Size(this.ClientSize.Width - 40, 120),
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(topContainer);

            // Date Picker
            Label dateLabel = new Label()
            {
                Text = "📅 DATE DU RAPPORT",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(0, 10),
                AutoSize = true,
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            topContainer.Controls.Add(dateLabel);

            selectedDate = new DateTimePicker()
            {
                Font = new Font("Arial", 14),
                Location = new Point(0, 40),
                Size = new Size(200, 40),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            selectedDate.ValueChanged += (s, e) => LoadDailyData();
            topContainer.Controls.Add(selectedDate);

            // Create the Cards
            CreateCards(topContainer);

            int gridTitleY = topContainer.Location.Y + topContainer.Height + 20;

            // ========== Grid Title ==========
            Label gridTitle = new Label()
            {
                Text = "📋 DÉTAIL DES VENTES DU JOUR - AVEC ID FACTURE",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(20, gridTitleY),
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 58, 138)
            };
            this.Controls.Add(gridTitle);

            // ========== Data Grid View ==========
            dailyGrid = new DataGridView()
            {
                Location = new Point(20, gridTitleY + gridTitle.Height + 10),
                Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - (gridTitleY + gridTitle.Height + 10) - 20),
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                Font = new Font("Arial", 11),
                RowTemplate = { Height = 40 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(dailyGrid);

            SetupGridColumns();

            // Reorder: Cards to the front
            topContainer.BringToFront();
        }

        private void CreateCards(Panel container)
        {
            // Modification: Déplacer les cartes vers la droite.
            // On augmente le point de départ et la distance entre les cartes
            int cardStartX = 350;
            int cardWidth = 330; // Réduire la largeur pour en accueillir 4
            int cardSpacing = 400; // Augmenter l'espacement pour les aligner correctement
            int cardY = 15; // Vertical position inside topContainer

            // --- 1. Revenue Card (CHIFFRE D'AFFAIRES) ---
            Panel revenueCard = new Panel()
            {
                Size = new Size(cardWidth, 105),
                BackColor = Color.FromArgb(255, 245, 225),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(cardStartX, cardY)
            };

            Label revenueTitle = new Label()
            {
                Text = "💰 C.A. NET",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(219, 119, 16),
                Location = new Point(15, 10),
                AutoSize = true
            };

            todayRevenueLabel = new Label()
            {
                Text = "0.00 €",
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(219, 119, 16),
                Location = new Point(15, 35),
                AutoSize = true,
                Height = 40
            };

            revenueCard.Controls.Add(revenueTitle);
            revenueCard.Controls.Add(todayRevenueLabel);
            container.Controls.Add(revenueCard);

            // --- 2. Sales Card (VENTES & RETOURNÉS) ---
            Panel salesCard = new Panel()
            {
                Size = new Size(cardWidth, 105),
                BackColor = Color.FromArgb(220, 230, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(cardStartX + cardSpacing, cardY)
            };

            Label salesTitle = new Label()
            {
                Text = "📦 VENTES / RETOURS (Qté)",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                Location = new Point(15, 10),
                AutoSize = true
            };

            todaySalesLabel = new Label()
            {
                Text = "0 vendus\n0 retournés",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                Location = new Point(15, 35),
                AutoSize = true,
                Height = 50
            };

            salesCard.Controls.Add(salesTitle);
            salesCard.Controls.Add(todaySalesLabel);
            container.Controls.Add(salesCard);

            // --- 3. Returns Card (MONTANT RETOURS) ---
            Panel returnsCard = new Panel()
            {
                Size = new Size(cardWidth, 105),
                BackColor = Color.FromArgb(255, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(cardStartX + cardSpacing * 2, cardY)
            };

            Label returnsTitle = new Label()
            {
                Text = "🔄 MONTANT RETOURS",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(15, 10),
                AutoSize = true
            };

            todayReturnsLabel = new Label()
            {
                Text = "0.00 €",
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(15, 35),
                AutoSize = true,
                Height = 40
            };

            returnsCard.Controls.Add(returnsTitle);
            returnsCard.Controls.Add(todayReturnsLabel);
            container.Controls.Add(returnsCard);

            // --- 4. NEW: Invoice Count Card (NOMBRE FACTURES) ---
            Panel invoiceCard = new Panel()
            {
                Size = new Size(cardWidth, 105),
                BackColor = Color.FromArgb(225, 255, 225),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(cardStartX + cardSpacing * 3, cardY)
            };

            Label invoiceTitle = new Label()
            {
                Text = "🧾 NBRE FACTURES",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 0),
                Location = new Point(15, 10),
                AutoSize = true
            };

            todayInvoicesLabel = new Label()
            {
                Text = "0 Factures",
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 0),
                Location = new Point(15, 35),
                AutoSize = true,
                Height = 40
            };

            invoiceCard.Controls.Add(invoiceTitle);
            invoiceCard.Controls.Add(todayInvoicesLabel);
            container.Controls.Add(invoiceCard);
        }

        private void SetupGridColumns()
        {
            if (dailyGrid == null) return;

            dailyGrid.Columns.Clear();
            dailyGrid.RowHeadersVisible = false;

            dailyGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "ID_Facture",
                HeaderText = "ID FACTURE",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    ForeColor = Color.DarkBlue
                }
            });

            dailyGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Produit",
                HeaderText = "PRODUIT",
                Width = 300,
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    Font = new Font("Arial", 11, FontStyle.Bold)
                }
            });

            dailyGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Quantite",
                HeaderText = "QUANTITÉ",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });

            dailyGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Prix",
                HeaderText = "PRIX (€)",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    Format = "N2",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            dailyGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Total",
                HeaderText = "TOTAL (€)",
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    Format = "N2",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            dailyGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Type",
                HeaderText = "TYPE",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });

            dailyGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Heure",
                HeaderText = "HEURE",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
        }

        private void LoadDailyData()
        {
            try
            {
                if (selectedDate == null) return;

                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                DateTime selectedDay = selectedDate.Value.Date;

                string statsQuery = @"
                    SELECT 
                        COALESCE(SUM(CASE WHEN quantity_sold > 0 THEN quantity_sold END), 0) as sold_products,
                        COALESCE(SUM(CASE WHEN quantity_sold < 0 THEN ABS(quantity_sold) END), 0) as returned_products,
                        COALESCE(SUM(CASE WHEN quantity_sold > 0 THEN total_amount END), 0) as total_sales,
                        COALESCE(SUM(CASE WHEN quantity_sold < 0 THEN ABS(total_amount) END), 0) as total_returns,
                        COALESCE(COUNT(DISTINCT id), 0) as invoice_count, -- NOUVEAU: Compte le nombre de factures distinctes
                        COALESCE(MIN(id), 0) as first_id,
                        COALESCE(MAX(id), 0) as last_id
                    FROM sales 
                    WHERE CAST(sale_date AS DATE) = @selectedDate";

                using var statsCmd = new NpgsqlCommand(statsQuery, con);
                statsCmd.Parameters.AddWithValue("@selectedDate", selectedDay);

                int soldProducts = 0;
                int returnedProducts = 0;
                decimal totalSales = 0;
                decimal totalReturns = 0;
                int invoiceCount = 0; // Nouvelle variable
                int firstId = 0;
                int lastId = 0;

                using var reader = statsCmd.ExecuteReader();
                if (reader.Read())
                {
                    soldProducts = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    returnedProducts = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    totalSales = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                    totalReturns = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3);
                    invoiceCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4); // Lecture de la nouvelle colonne
                    firstId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                    lastId = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);
                }
                reader.Close();

                decimal netRevenue = Math.Max(0, totalSales - totalReturns);

                // Mise à jour des cartes

                if (todayRevenueLabel != null)
                    todayRevenueLabel.Text = $"{netRevenue:N2} €";

                if (todaySalesLabel != null)
                {
                    // La section idInfo a été retirée de salesLabel pour faire de la place.
                    todaySalesLabel.Text = $"{soldProducts} vendus\n{returnedProducts} retournés";
                }

                if (todayReturnsLabel != null)
                    todayReturnsLabel.Text = $"{totalReturns:N2} €";

                // Mise à jour de la NOUVELLE carte Factures
                if (todayInvoicesLabel != null)
                    todayInvoicesLabel.Text = $"{invoiceCount} Factures";


                LoadSalesDetails(con, selectedDay);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur chargement rapport: {ex.Message}");
                // Afficher des zéros si la connexion échoue
                if (todayRevenueLabel != null) todayRevenueLabel.Text = "0.00 €";
                if (todaySalesLabel != null) todaySalesLabel.Text = "0 vendus\n0 retournés";
                if (todayReturnsLabel != null) todayReturnsLabel.Text = "0.00 €";
                if (todayInvoicesLabel != null) todayInvoicesLabel.Text = "0 Factures";
                if (dailyGrid != null) dailyGrid.Rows.Clear();
            }
        }

        private void LoadSalesDetails(NpgsqlConnection con, DateTime selectedDay)
        {
            // Code existant pour LoadSalesDetails...
            try
            {
                if (dailyGrid == null) return;

                string salesQuery = @"
                    SELECT 
                        id,
                        product_name,
                        quantity_sold,
                        unit_price,
                        total_amount,
                        CASE 
                            WHEN quantity_sold < 0 THEN '🔄 RETOUR'
                            ELSE '✅ VENTE'
                        END as type,
                        TO_CHAR(sale_date, 'HH24:MI') as time
                    FROM sales
                    WHERE CAST(sale_date AS DATE) = @selectedDate
                    ORDER BY id DESC, sale_date DESC";

                using var cmd = new NpgsqlCommand(salesQuery, con);
                cmd.Parameters.AddWithValue("@selectedDate", selectedDay);

                using var reader = cmd.ExecuteReader();

                dailyGrid.Rows.Clear();

                while (reader.Read())
                {
                    int rowIndex = dailyGrid.Rows.Add();
                    DataGridViewRow row = dailyGrid.Rows[rowIndex];

                    int quantity = reader.GetInt32(2);
                    string type = reader.GetString(5);

                    row.Cells["ID_Facture"].Value = reader.GetInt32(0);
                    row.Cells["Produit"].Value = reader.GetString(1);
                    row.Cells["Quantite"].Value = Math.Abs(quantity);
                    row.Cells["Prix"].Value = reader.GetDecimal(3);
                    row.Cells["Total"].Value = Math.Abs(reader.GetDecimal(4));
                    row.Cells["Type"].Value = type;
                    row.Cells["Heure"].Value = reader.GetString(6);

                    if (quantity < 0)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(200, 0, 0);
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 100, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur détails ventes: {ex.Message}");
            }
        }

        private void EnableSecretClickOnTitle()
        {
            // Code existant pour EnableSecretClickOnTitle...
            if (titleLabel == null) return;

            int clickCount = 0;
            DateTime lastClickTime = DateTime.MinValue;

            titleLabel.MouseClick += (s, e) =>
            {
                DateTime now = DateTime.Now;

                if ((now - lastClickTime).TotalSeconds > 2)
                    clickCount = 0;

                clickCount++;
                lastClickTime = now;

                titleLabel.BackColor = Color.FromArgb(100, 255, 255, 255);

                var resetTimer = new System.Windows.Forms.Timer() { Interval = 200 };
                resetTimer.Tick += (_, __) =>
                {
                    titleLabel.BackColor = Color.Transparent;
                    resetTimer.Stop();
                    resetTimer.Dispose();
                };
                resetTimer.Start();

                if (clickCount >= 3)
                {
                    clickCount = 0;
                    OuvrirEcranSuppression();
                }
            };
        }

        private void OuvrirEcranSuppression()
        {
            // Code existant pour OuvrirEcranSuppression...
            if (selectedDate == null) return;

            DateTime selectedDay = selectedDate.Value.Date;

            DialogResult result = MessageBox.Show(
                $"⚠️ Voulez-vous vraiment supprimer le rapport du {selectedDay:dd/MM/yyyy}?\n\nCette action est irréversible!",
                "🔓 SUPPRESSION RAPPORT",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
                SupprimerRapportDuJour();
        }

        private void SupprimerRapportDuJour()
        {
            // Code existant pour SupprimerRapportDuJour...
            if (selectedDate == null) return;

            DateTime selectedDay = selectedDate.Value.Date;

            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                string deleteQuery = @"DELETE FROM sales 
                    WHERE CAST(sale_date AS DATE) = @selectedDate";

                using var cmd = new NpgsqlCommand(deleteQuery, con);
                cmd.Parameters.AddWithValue("@selectedDate", selectedDay);

                int rowsAffected = cmd.ExecuteNonQuery();

                LoadDailyData();

                MessageBox.Show(
                    $"✅ Rapport du {selectedDay:dd/MM/yyyy} supprimé!\n{rowsAffected} lignes effacées.",
                    "Suppression Réussie",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erreur lors de la suppression:\n{ex.Message}",
                    "Erreur de Suppression",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void RefreshReport()
        {
            LoadDailyData();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                refreshTimer?.Stop();
                refreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}