using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;

namespace Activity4_Joson
{
    public partial class DashboardForm : Form
    {
        private int selectedProductID = 0;
        public DashboardForm()
        {
            InitializeComponent();

            btnAdd.Click += btnAdd_Click;
            btnUpdate.Click += btnUpdate_Click;
            btnDelete.Click += btnDelete_Click;
            btnClear.Click += btnClear_Click;
            btnRefresh.Click += btnRefresh_Click;

            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;

            txtSearch.TextChanged += txtSearch_TextChanged;
            dgvProducts.CellClick += dgvProducts_CellClick;

            LoadProducts();
            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            try
            {
                using (OleDbConnection con = Database.GetConnection())
                {
                    con.Open();

                    string query = @"SELECT COUNT(*) AS TotalProducts, 
                            SUM(IIF(Quantity > ReorderLevel, 1 ,0)) AS InStock, 
                            SUM(IIF(Quantity > 0 AND Quantity <= ReorderLevel, 1 , 0)) AS LowStock, 
                            SUM(IIF(Quantity = 0, 1, 0)) AS OutOfStock 
                            FROM Products";
                    using (OleDbCommand cmd = new OleDbCommand(query, con))
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            lblTotalCount.Text = reader["TotalProducts"].ToString();
                            lblInStockCount.Text = reader["InStock"].ToString();
                            lblLowStockCount.Text = reader["LowStock"].ToString();
                            lblOutOfStockCount.Text = reader["OutOfStock"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Statistic error: " + ex.Message);
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (OleDbConnection con = Database.GetConnection())
                {
                    con.Open();

                    string query = @"SELECT * FROM Products ORDER BY ProductID DESC";

                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();

                        adapter.Fill(dt);

                        dgvProducts.AutoGenerateColumns = true;
                        dgvProducts.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load products:" + ex.Message);
            }
        }

        private bool ValidateProduct()
        {
            if (string.IsNullOrEmpty(txtProductName.Text))
            {
                MessageBox.Show("Please enter a product name.");
                txtProductName.Focus();
                return false;
            }

            if (cmbCategory.SelectedIndex == -1)
            {
                MessageBox.Show("Please enter a product category.");
                cmbCategory.Focus();
                return false;
            }

            decimal price;

            if (!decimal.TryParse(txtUnitPrice.Text, out price) || price < 0)
            {
                MessageBox.Show("Please enter a product price.");
                txtUnitPrice.Focus();
                return false;
            }

            int quantity;

            if (!int.TryParse(txtQuantity.Text, out quantity) || quantity < 0)
            {
                MessageBox.Show("Please enter quantity.");
                txtQuantity.Focus();
                return false;
            }

            int reorder;

            if (!int.TryParse(txtReorderLevel.Text, out reorder) || reorder < 0)
            {
                MessageBox.Show("Please enter reorder level.");
                txtReorderLevel.Focus();
                return false;
            }

            return true;
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedProductID == 0)
            {
                MessageBox.Show("Please select a product row to update.");
                return;
            }

            if (!ValidateProduct())
                return;

            try
            {
                using (OleDbConnection con = Database.GetConnection())
                {
                    con.Open();

                    string query = @"UPDATE Products SET ProductName=?, Category=?, Description=?, UnitPrice=?, Quantity=?, ReorderLevel=? WHERE ProductID=?";

                    using (OleDbCommand cmd = new OleDbCommand(query, con))
                    {
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = txtProductName.Text.Trim();
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = cmbCategory.Text;
                        cmd.Parameters.Add("?", OleDbType.LongVarWChar).Value = txtDescription.Text.Trim();
                        cmd.Parameters.Add("?", OleDbType.Currency).Value = decimal.Parse(txtUnitPrice.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = int.Parse(txtQuantity.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = int.Parse(txtReorderLevel.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = selectedProductID;

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product updated successfully!", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearField();
                LoadDashboardStats();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating product:" + ex.Message);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedProductID == 0)
            {
                MessageBox.Show("Please select a product row to delete.");
                return;
            }

            DialogResult result = MessageBox.Show("Are you sure you want to delete this product?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (OleDbConnection con = Database.GetConnection())
                    {
                        con.Open();

                        string query = @"DELETE FROM Products WHERE ProductID = ?";

                        using (OleDbCommand cmd = new OleDbCommand(query, con))
                        {
                            cmd.Parameters.Add("?", OleDbType.Integer).Value = selectedProductID;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Product deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearField();
                    LoadProducts();
                    LoadDashboardStats();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting product: " + ex.Message);
                }
            }
        }

        private void dgvProducts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvProducts.Rows[e.RowIndex];

                selectedProductID = Convert.ToInt32(row.Cells["ProductID"].Value);

                txtProductName.Text = row.Cells["ProductName"].Value.ToString();
                cmbCategory.Text = row.Cells["Category"].Value.ToString();
                txtDescription.Text = row.Cells["Description"].Value.ToString();
                txtUnitPrice.Text = row.Cells["UnitPrice"].Value.ToString();
                txtQuantity.Text = row.Cells["Quantity"].Value.ToString();
                txtReorderLevel.Text = row.Cells["ReorderLevel"].Value.ToString();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateProduct())
                return;

            try
            {
                using (OleDbConnection con = Database.GetConnection())
                {
                    con.Open();

                    string query = @"INSERT INTO Products (ProductName, Category, Description, UnitPrice, Quantity, ReorderLevel, CreatedAt) VALUES (?,?,?,?,?,?, Now())";

                    using (OleDbCommand cmd = new OleDbCommand(query, con))
                    {
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = txtProductName.Text.Trim();
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = cmbCategory.Text;
                        cmd.Parameters.Add("?", OleDbType.LongVarWChar).Value = txtDescription.Text.Trim();
                        cmd.Parameters.Add("?", OleDbType.Currency).Value = decimal.Parse(txtUnitPrice.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = int.Parse(txtQuantity.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = int.Parse(txtReorderLevel.Text);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product added successfully!", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearField();
                LoadProducts();
                LoadDashboardStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding product:" + ex.Message);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearField();
        }

        private void ClearField()
        {
            txtProductName.Clear();
            cmbCategory.SelectedIndex = -1;
            txtDescription.Clear();
            txtUnitPrice.Clear();
            txtQuantity.Clear();
            txtReorderLevel.Clear();

            selectedProductID = 0;

            dgvProducts.ClearSelection();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            ClearField();
            LoadProducts();
            LoadDashboardStats();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try
            {
                using (OleDbConnection con = Database.GetConnection())
                {
                    con.Open();

                    string query = @"SELECT * FROM Products WHERE ProductName LIKE ? OR Category LIKE ? ORDER BY ProductID DESC";

                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(query, con))
                    {
                        string keyword = "%" + txtSearch.Text.Trim() + "%";
                        adapter.SelectCommand.Parameters.Add("?", OleDbType.VarWChar).Value = keyword;
                        adapter.SelectCommand.Parameters.Add("?", OleDbType.VarWChar).Value = keyword;

                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvProducts.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search error: " + ex.Message);
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                SignInForm login = new SignInForm();
                login.Show();

                this.Hide();
            }
        }

        private void DashboardForm_Load(object sender, EventArgs e)
        {
            this.productsTableAdapter.Fill(this.sampledbDataSet.Products);
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
