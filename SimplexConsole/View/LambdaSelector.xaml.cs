using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Amazon.Lambda;
using Amazon;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Amazon.Lambda.Model;

namespace SimplexConsole.View
{
    /// <summary>
    /// Interaction logic for InitScreen.xaml
    /// </summary>
    public partial class LambdaSelector : Window
    {
        public DependencyProperty SelectEnabledProp = DependencyProperty.Register
            ("SelectEnabled", typeof(bool), typeof(LambdaSelector));

        public AmazonLambdaConfig LambdaConfig { get; private set; }
        public AmazonLambdaClient LambdaClient { get; private set; }
        public FunctionConfiguration SelectedFunction { get; private set; }

        public bool SelectEnabled
        {
            get { return (bool)GetValue(SelectEnabledProp); }
            set { SetValue(SelectEnabledProp, value); }
        }

        List<string> endpoints
        {
            get 
            {
                List<string> list = new List<string>();
                foreach (var edp in Amazon.RegionEndpoint.EnumerableAllRegions)
                    list.Add(edp.DisplayName);
                return list;
            }
        }

        public ObservableCollection<FunctionConfiguration> Lambdas
        {
            get;
        } = new ObservableCollection<FunctionConfiguration>();

        public LambdaSelector()
        {
            LambdaConfig = new AmazonLambdaConfig();

            InitializeComponent();

            CmbRegions.ItemsSource = endpoints;
            SelectEnabled = false;
        }

        private void ConfigReset()
        {
            LambdaClient = new AmazonLambdaClient(LambdaConfig);
            Lambdas.Clear();
            LambdaClient.ListFunctionsAsync()
                .ContinueWith(
                    (task) =>
                    {
                        foreach (var f in task.Result.Functions)
                        {
                            Lambdas.Add(f);
                        }
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
            SelectEnabled = false;
        }

        private void CmbRegions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LambdaConfig.RegionEndpoint = Enumerable.ElementAt(RegionEndpoint.EnumerableAllRegions, CmbRegions.SelectedIndex);
            ConfigReset();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectEnabled = true;
            SelectedFunction = (FunctionConfiguration)ListFunctions.SelectedItem;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
