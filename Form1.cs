using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.Threading;

namespace Faktury
{
    public partial class Form1 : Form
    {
        string connectionString = @"Data Source=(localdb)\mssqllocaldb;Initial Catalog=baza;Integrated Security=True";

        public Form1()
        {
            InitializeComponent();
            CultureInfo customCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = customCulture;
            Thread.CurrentThread.CurrentUICulture = customCulture;
            textBox1.Enter += textBox1_Enter;
            textBox1.Leave += textBox1_Leave;
            dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
            dataGridView1.ReadOnly = true;
            //reset();
            refresh();
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if (textBox1.Text == "Numer faktury")
                textBox1.Clear();
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
                textBox1.Text = "Numer faktury";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void refresh()
        {
            string select = "SELECT * FROM invoice";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(select, connection))
                {
                    var myDataSet = new DataSet();
                    adapter.Fill(myDataSet);
                    dataGridView1.DataSource = myDataSet.Tables[0];
                    dataGridView1.Columns["value"].DefaultCellStyle.FormatProvider = CultureInfo.InvariantCulture;
                    dataGridView1.Columns["value"].DefaultCellStyle.Format = "N2";
                }
            }
        }

        private void reset()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    using (SqlCommand deleteCommand = new SqlCommand("DELETE FROM invoice_pos", connection, transaction))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }
                    using (SqlCommand reseedCommand = new SqlCommand("DBCC CHECKIDENT ('invoice_pos', RESEED, 0)", connection, transaction))
                    {
                        reseedCommand.ExecuteNonQuery();
                    }
                    using (SqlCommand deleteCommand = new SqlCommand("DELETE FROM invoice", connection, transaction))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }
                    using (SqlCommand reseedCommand = new SqlCommand("DBCC CHECKIDENT ('invoice', RESEED, 0)", connection, transaction))
                    { 
                        reseedCommand.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 1)
            {
                DataGridViewRow row = dataGridView1.SelectedRows[0];
                if (!row.IsNewRow)
                {
                    string invoice_id = dataGridView1.SelectedRows[0].Cells["invoice_id"].Value.ToString();
                    string select = "SELECT * FROM invoice_pos WHERE invoice_id = @invoice_id";
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(select, connection))
                        {
                            adapter.SelectCommand.Parameters.AddWithValue("@invoice_id", invoice_id);
                            var myDataSet = new DataSet();
                            adapter.Fill(myDataSet);
                            dataGridView2.DataSource = myDataSet.Tables[0];
                            dataGridView2.Columns["value"].DefaultCellStyle.FormatProvider = CultureInfo.InvariantCulture;
                            dataGridView2.Columns["value"].DefaultCellStyle.Format = "N2";
                        }
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    if (int.TryParse(textBox1.Text, out int result))
                    {
                        string query = "INSERT INTO invoice (number, value) VALUES (@number, 0)";
                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@number", textBox1.Text);
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                        MessageBox.Show("Cannot add this value!");
                    transaction.Commit();
                }
            }
            textBox1.Text = "Numer faktury";
            refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 1)
            {
                string id = dataGridView1.SelectedRows[0].Cells["invoice_id"].Value.ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                        {
                            if (!row.IsNewRow)
                            {
                                string column1Value = row.Cells["invoice_id"].Value.ToString();
                                string queryPos = "DELETE FROM invoice_pos WHERE invoice_id=@invoice_id";
                                using (SqlCommand command1 = new SqlCommand(queryPos, connection, transaction))
                                {
                                    command1.Parameters.AddWithValue("@invoice_id", column1Value);
                                    command1.ExecuteNonQuery();
                                }
                                string query = "DELETE FROM invoice WHERE invoice_id=@invoice_id";
                                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@invoice_id", column1Value);
                                    command.ExecuteNonQuery();
                                }
                            }
                            else
                                MessageBox.Show("Cannot delete this row!");
                        }
                        transaction.Commit();
                    }
                }
                refresh();
                refresh1(id);
            }
            else
                MessageBox.Show("Choose an invoice!");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 1)
            {
                string id = dataGridView1.SelectedRows[0].Cells["invoice_id"].Value.ToString();
                decimal sum = 0;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        foreach (DataGridViewRow row in dataGridView2.Rows)
                        {
                            if (!row.IsNewRow && string.IsNullOrWhiteSpace(row.Cells["invoice_pos_id"].Value.ToString()))
                            {
                                try
                                {
                                    string valueString = row.Cells["value"].Value.ToString();
                                    if (!double.TryParse(valueString, out double result))
                                        throw new Exception("Wrong value!");
                                    string column1Value = dataGridView1.SelectedRows[0].Cells["invoice_id"].Value.ToString();
                                    string column2Value = row.Cells["name"].Value.ToString();
                                    string query = "INSERT INTO invoice_pos (invoice_id, name, value) VALUES (@invoice_id, @name, @value)";
                                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@invoice_id", column1Value);
                                        command.Parameters.AddWithValue("@name", column2Value);
                                        SqlParameter valueParameter = new SqlParameter("@value", SqlDbType.Decimal)
                                        {
                                            Precision = 10,
                                            Scale = 2,
                                            Value = result
                                        };
                                        command.Parameters.Add(valueParameter);
                                        command.ExecuteNonQuery();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    MessageBox.Show(ex.Message);
                                    return;
                                }
                            }
                        }
                        foreach (DataGridViewRow row in dataGridView2.Rows)
                        {
                            if (!row.IsNewRow)
                            {
                                string valueString = row.Cells["value"].Value.ToString();
                                decimal.TryParse(valueString, out decimal result);
                                sum += result;
                            }
                        }
                        string update = "UPDATE invoice SET value=@value WHERE invoice_id=@invoice_id";
                        using (SqlCommand command = new SqlCommand(update, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@invoice_id", id);
                            SqlParameter valueParameter = new SqlParameter("@value", SqlDbType.Decimal)
                            {
                                Precision = 10,
                                Scale = 2,
                                Value = sum
                            };
                            command.Parameters.Add(valueParameter);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
                refresh();
                refresh1(id);
            }
            else
                MessageBox.Show("Choose an invoice!");
        }
        
        private void refresh1(string id)
        {
            string select = "SELECT * FROM invoice_pos WHERE invoice_id=@invoice_id";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(select, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@invoice_id", id);
                    var myDataSet = new DataSet();
                    adapter.Fill(myDataSet);
                    dataGridView2.DataSource = myDataSet.Tables[0];
                    dataGridView2.Columns["value"].DefaultCellStyle.FormatProvider = CultureInfo.InvariantCulture;
                    dataGridView2.Columns["value"].DefaultCellStyle.Format = "N2";
                }
            }
        }
        
        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 1)
            {
                string id = dataGridView1.SelectedRows[0].Cells["invoice_id"].Value.ToString();
                string valueString = dataGridView1.SelectedRows[0].Cells["value"].Value.ToString();
                decimal.TryParse(valueString, out decimal sum);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        foreach (DataGridViewRow row in dataGridView2.SelectedRows)
                        {
                            if (!row.IsNewRow)
                            {
                                string column1Value = row.Cells["invoice_pos_id"].Value.ToString();
                                string column2Value = row.Cells["value"].Value.ToString();
                                decimal.TryParse(column2Value, out decimal result);
                                sum -= result;
                                string query = "DELETE FROM invoice_pos WHERE invoice_pos_id=@invoice_pos_id";
                                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@invoice_pos_id", column1Value);
                                    command.ExecuteNonQuery();
                                }
                            }
                            else
                                MessageBox.Show("Cannot delete this row!");
                        }
                        string update = "UPDATE invoice SET value=@value WHERE invoice_id=@invoice_id";
                        using (SqlCommand command = new SqlCommand(update, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@invoice_id", id);
                            SqlParameter valueParameter = new SqlParameter("@value", SqlDbType.Decimal)
                            {
                                Precision = 10,
                                Scale = 2,
                                Value = sum
                            };
                            command.Parameters.Add(valueParameter);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
                refresh1(id);
                refresh();
            }
            else
                MessageBox.Show("Choose an invoice!");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int result) && dataGridView1.SelectedRows.Count == 1)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        string invoice_id = dataGridView1.SelectedRows[0].Cells["invoice_id"].Value.ToString();
                        string update = "UPDATE invoice SET number=@number WHERE invoice_id=@invoice_id";
                        using (SqlCommand command = new SqlCommand(update, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@invoice_id", invoice_id);
                            command.Parameters.AddWithValue("@number", textBox1.Text);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
            }
            else
            {
                MessageBox.Show("No value to update");
                refresh();
                return;
            }
            textBox1.Text = "Numer faktury";
            refresh();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 1)
            {
                string id = dataGridView1.SelectedRows[0].Cells["invoice_id"].Value.ToString();
                decimal sum = 0;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        foreach (DataGridViewRow row in dataGridView2.Rows)
                        {
                            if (!row.IsNewRow)
                            {
                                try
                                {
                                    string valueString = row.Cells["value"].Value.ToString();
                                    if (!decimal.TryParse(valueString, out decimal result))
                                        throw new Exception("Wrong value!");
                                    string invoice_pos_id = row.Cells["invoice_pos_id"].Value.ToString();
                                    string column1Value = row.Cells["name"].Value.ToString();
                                    sum += result;
                                    string update = "UPDATE invoice_pos SET name=@name, value=@value WHERE invoice_pos_id=@invoice_pos_id";
                                    using (SqlCommand command = new SqlCommand(update, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@invoice_pos_id", invoice_pos_id);
                                        command.Parameters.AddWithValue("@name", column1Value);
                                        SqlParameter valueParameter = new SqlParameter("@value", SqlDbType.Decimal)
                                        {
                                            Precision = 10,
                                            Scale = 2,
                                            Value = result
                                        };
                                        command.Parameters.Add(valueParameter);
                                        command.ExecuteNonQuery();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    MessageBox.Show(ex.Message);
                                    return;
                                }
                            }
                        }
                        string update1 = "UPDATE invoice SET value=@value WHERE invoice_id=@invoice_id";
                        using (SqlCommand command = new SqlCommand(update1, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@invoice_id", id);
                            SqlParameter valueParameter = new SqlParameter("@value", SqlDbType.Decimal)
                            {
                                Precision = 10,
                                Scale = 2,
                                Value = sum
                            };
                            command.Parameters.Add(valueParameter);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
                refresh();
                refresh1(id);
            }
            else
                MessageBox.Show("Choose an invoice!");
        }
    }
}




