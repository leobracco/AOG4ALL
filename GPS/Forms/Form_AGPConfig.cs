using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using static AgOpenGPS.FormGPS;

namespace AgOpenGPS.Forms
{
    public partial class Form_AGPConfig : Form
    {
        private FormGPS _formGPS;
        public Form_AGPConfig(FormGPS formGPS)
        {
            InitializeComponent();
            _formGPS = formGPS;
            LoadSensorConfig();
        }
        private void myButton_Click(object sender, EventArgs e)
        {
            var data = new
            {
                NumberOfSensors = txtCantidadSensores.Text,
                KgPorHa = txtKgPorHa.Text,
                
            };

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText("config.json", json);
            _formGPS.LoadAndApplySensorConfig();
            MessageBox.Show("Configuración guardada.");
            this.Close();
        }
        private void LoadSensorConfig()
        {
            try
            {
                string filePath = "config.json";
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var config = JsonConvert.DeserializeObject<SensorConfig>(json);
                    txtCantidadSensores.Text = config.NumberOfSensors.ToString();
                    txtKgPorHa.Text = config.KgPerHa.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la configuración: " + ex.Message);
            }
        }
        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txtCantidadSensores_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
    
}
