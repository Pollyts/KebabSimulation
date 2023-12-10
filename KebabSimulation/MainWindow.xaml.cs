using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KebabSimulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static int speed = 100;
        public delegate void SayHiDelegate(string msg);
        public MainWindow()
        {
            InitializeComponent();    
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {            
            speed = Convert.ToInt32(Speed.Text);

            var sideDishes = SideDishNames.Text.Split('/');
            var meatDishes = MeatDishNames.Text.Split('/');            

            var customerMeatChoises = ConvertToDoubleArray(CustomerMeatChoise.Text.Split("/"));

            var customerSideDishChoises = ConvertToDoubleArray(CustomerSideDishChoise.Text.Split("/"));

            var customerLeavePercent = ConvertToDoubleArray(new string[] { CustomerLeavePercent.Text, (Convert.ToDouble(1) - double.Parse(CustomerLeavePercent.Text, NumberStyles.Float, CultureInfo.InvariantCulture)).ToString().Replace(',', '.') });

            //time cooking in mins
            var cookingTimeMeat = new TimeSpan[meatDishes.Length];
            var cookingTimesMeats = ConvertToDoubleArray(CookingTimeMeat.Text.Split("/"));
            for (int i = 0; i< cookingTimeMeat.Length; i++)
            {
                cookingTimeMeat[i] = new TimeSpan(0, Convert.ToInt32(cookingTimesMeats[i]), 0);
            }

            var cookingTimeSideDish = new TimeSpan[sideDishes.Length];
            var cookingTimesSideDishes = ConvertToDoubleArray(CookingTimeSideDish.Text.Split("/"));
            for (int i = 0; i < cookingTimeSideDish.Length; i++)
            {
                cookingTimeSideDish[i] = new TimeSpan(0, Convert.ToInt32(cookingTimesSideDishes[i]), 0);
            }

            var sideDishWeight = ConvertToDoubleArray(SideDishWeight.Text.Split("/"));
            var meatDishWeight = ConvertToDoubleArray(MeatDishWeight.Text.Split("/"));

            var currentSideDishCount = ConvertToDoubleArray(SideDishStartCount.Text.Split("/"));
            var currentMeatDishCount = ConvertToDoubleArray(MeatDishStartCount.Text.Split("/"));

            DateTime?[] sideDishTimeReady = new DateTime?[currentSideDishCount.Length];
            DateTime?[] meatDishTimeReady = new DateTime?[currentMeatDishCount.Length];

            double[] sideDishWeightReady = new double[currentSideDishCount.Length];
            double[] meatDishWeightReady = new double[currentMeatDishCount.Length];

            //some statistics
            int[] sideDishSoldCount = new int[currentSideDishCount.Length];
            int[] meatDishSoldCount = new int[currentMeatDishCount.Length];

            int[] sideDishMissingCount = new int[currentSideDishCount.Length];
            int[] meatDishMissingCount = new int[currentMeatDishCount.Length];

            int clientsLeaveCount = 0;

            int clientId = 1;            

            for (int currentHour = 10; currentHour < 21; currentHour++)
            {
                //how many customers come for an hour
                var customersCount = Convert.ToInt32(((TextBox)this.FindName($"CustomersCountAt{currentHour}")).Text);

                //var customersCount = ConvertToDoubleArray(((TextBox)this.FindName($"CustomersCountAt{currentHour}")).Text);

                var customerDistribution = RandomCustomerDistribution(customersCount);

                for (int currentCustomerId = 0; currentCustomerId < customersCount; currentCustomerId++)
                {
                    var currentTime = DateTime.Today.Add(new TimeSpan(currentHour, customerDistribution[currentCustomerId], 0));
                    var selectedMeat = CustomerChoose(customerMeatChoises);
                    var selectedSideDish = CustomerChoose(customerSideDishChoises);

                    await WriteLogEvent(string.Format("{0}:{1} New Customer{2} has arrived! He wants {3} with {4}", currentTime.ToShortDateString(), currentTime.ToShortTimeString(), clientId, sideDishes[selectedSideDish], meatDishes[selectedMeat]));
                    

                    //sell dish

                    if((currentSideDishCount[selectedSideDish] - sideDishWeight[selectedSideDish] > 0) && (currentMeatDishCount[selectedMeat] - meatDishWeight[selectedMeat] > 0))
                    {
                        currentSideDishCount[selectedSideDish] -= sideDishWeight[selectedSideDish];
                        sideDishSoldCount[selectedSideDish]++;

                        currentMeatDishCount[selectedMeat] -= meatDishWeight[selectedMeat];
                        meatDishSoldCount[selectedMeat]++;
                    }
                    else
                    {
                        var isCustomerStay = CustomerChoose(customerLeavePercent);
                        if (isCustomerStay == 1
                            && (((currentMeatDishCount[selectedMeat] - meatDishWeight[selectedMeat] < 0) 
                            && meatDishTimeReady[selectedMeat] != null) 
                            || ((currentSideDishCount[selectedSideDish] - sideDishWeight[selectedSideDish] < 0)
                            && sideDishTimeReady[selectedSideDish] != null)))
                        {
                            if (currentSideDishCount[selectedSideDish] - sideDishWeight[selectedSideDish] < 0)
                            {
                                await WriteLogEvent(string.Format("WARNING! The side dish {0} is missing! But the client decided to wait", sideDishes[selectedSideDish]));
                            }
                            if (currentMeatDishCount[selectedMeat] - meatDishWeight[selectedMeat] < 0)
                            {
                                await WriteLogEvent(string.Format("WARNING! The meat {0} is missing! But the client decided to wait", meatDishes[selectedMeat]));
                            }
                            currentSideDishCount[selectedSideDish] -= sideDishWeight[selectedSideDish];
                            sideDishSoldCount[selectedSideDish]++;

                            currentMeatDishCount[selectedMeat] -= meatDishWeight[selectedMeat];
                            meatDishSoldCount[selectedMeat]++;
                        }
                        else
                        {
                            clientsLeaveCount++;
                            if (currentSideDishCount[selectedSideDish] - sideDishWeight[selectedSideDish] < 0)
                            {
                                sideDishMissingCount[selectedSideDish]++;
                                await WriteLogEvent(string.Format("WARNING! The side dish {0} is missing! The client leaves", sideDishes[selectedSideDish]));
                            }
                            if (currentMeatDishCount[selectedMeat] - meatDishWeight[selectedMeat] < 0)
                            {
                                meatDishMissingCount[selectedMeat]++;
                                await WriteLogEvent(string.Format("WARNING! The meat {0} is missing! The client leaves", meatDishes[selectedMeat]));
                            }
                        }
                    }
                    clientId++;

                    //check if we need to start to cook dish
                    var meatLeft = ConvertToDoubleArray(((TextBox)this.FindName($"MeatLeftAt{currentHour}")).Text.Split("/"));
                    var sideDishLeft = ConvertToDoubleArray(((TextBox)this.FindName($"SideDishLeftAt{currentHour}")).Text.Split("/"));

                    var meatCook = ConvertToDoubleArray(((TextBox)this.FindName($"MakeMeatAt{currentHour}")).Text.Split("/"));
                    var sideDishCook = ConvertToDoubleArray(((TextBox)this.FindName($"MakeSideDishAt{currentHour}")).Text.Split("/"));

                    if (currentMeatDishCount[selectedMeat] < meatLeft[selectedMeat] && meatDishTimeReady[selectedMeat] == null)
                    {
                        meatDishTimeReady[selectedMeat] = currentTime.Add(cookingTimeMeat[selectedMeat]);
                        meatDishWeightReady[selectedMeat] = meatCook[selectedMeat];
                        await WriteLogEvent(string.Format("INFO: The meat {0} running out! {1}kg will be cooked at {2}", meatDishes[selectedMeat], meatDishWeightReady[selectedMeat].ToString(), meatDishTimeReady[selectedMeat].Value.ToShortTimeString()));
                    }

                    if (currentSideDishCount[selectedSideDish] < sideDishLeft[selectedSideDish] && sideDishTimeReady[selectedSideDish] == null)
                    {
                        sideDishTimeReady[selectedSideDish] = currentTime.Add(cookingTimeSideDish[selectedSideDish]);
                        sideDishWeightReady[selectedSideDish] = sideDishCook[selectedSideDish];
                        await WriteLogEvent(string.Format("INFO: The side dish {0} running out! {1}kg will be cooked at {2}", sideDishes[selectedSideDish], sideDishWeightReady[selectedSideDish].ToString(), sideDishTimeReady[selectedSideDish].Value.ToShortTimeString()));
                    }


                    //check if dish was cooked
                    for (int i = 0; i < meatDishes.Length; i++)
                    {
                        if (meatDishTimeReady[i]!=null && currentTime>meatDishTimeReady[i])
                        {
                            meatDishTimeReady[i] = null;
                            currentMeatDishCount[i] += meatDishWeightReady[i];
                            meatDishWeightReady[i] = 0;
                            await WriteLogEvent(string.Format("INFO: The meat dish {0} was cooked!", meatDishes[selectedMeat]));
                        }                        
                    }

                    for (int i = 0; i < sideDishes.Length; i++)
                    {
                        if (sideDishTimeReady[i] != null && currentTime > sideDishTimeReady[i])
                        {
                            sideDishTimeReady[i] = null;
                            currentSideDishCount[i] += sideDishWeightReady[i];
                            sideDishWeightReady[i] = 0;
                            await WriteLogEvent(string.Format("INFO: The meat dish {0} was cooked!", sideDishes[selectedSideDish]));
                        }
                    }
                }
            }

            await WriteLogEvent("-------STATISTIC--------");

            //int[] sideDishSoldCount = new int[currentSideDishCount.Length];
            //int[] meatDishSoldCount = new int[currentMeatDishCount.Length];

            //int[] sideDishMissingCount = new int[currentSideDishCount.Length];
            //int[] meatDishMissingCount = new int[currentMeatDishCount.Length];

            //int clientsLeaveCount = 0;

            for(int i=0; i<meatDishes.Length; i++)
            {
                await WriteLogEvent(string.Format("--{0}--", meatDishes[i]));
                await WriteLogEvent(string.Format("Total sold: {0}", meatDishSoldCount[i]));
                await WriteLogEvent(string.Format("Additional would be sold if there were enough: {0}", meatDishMissingCount[i]));
                await WriteLogEvent(string.Format("Kilo left: {0}", currentMeatDishCount[i].ToString("0.00")));
            }

            for (int i = 0; i < sideDishes.Length; i++)
            {
                await WriteLogEvent(string.Format("--{0}--", sideDishes[i]));
                await WriteLogEvent(string.Format("Total sold: {0}", sideDishSoldCount[i]));
                await WriteLogEvent(string.Format("Additional would be sold if there were enough: {0}", sideDishMissingCount[i]));
                await WriteLogEvent(string.Format("Kilo left: {0}", currentSideDishCount[i].ToString("0.00")));
            }

            await WriteLogEvent(string.Format("---Clients leave count: {0} ---", clientsLeaveCount));

            await WriteLogEvent(string.Format("The simulation model was created by Polina Tselischeva (mail: apolinats@gmail.com; github: Pollyts)"));

        }

        private async Task WriteLogEvent(string message)
        {
            Run run = new Run(message);
            Paragraph paragraph = new Paragraph(run);
            LogTextBox.Document.Blocks.Add(paragraph);
            LogTextBox.ScrollToEnd();
            await Task.Delay(speed);
        }

        public static int CustomerChoose(double[] probabilities)
        {
            // Индекс области, в которую попало случайное число
            int chosenArea = 0;

            double randomValue = new Random().NextDouble();

            // Суммирование вероятностей до тех пор, пока не будет достигнуто случайное число
            double cumulativeProbability = 0;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulativeProbability += probabilities[i];
                if (randomValue < cumulativeProbability)
                {
                    chosenArea = i;
                    break;
                }
            }

            return chosenArea;
        }

        static double[] ConvertToDoubleArray(string[] stringArray)
        {
            // Инициализация нового массива чисел с плавающей точкой
            double[] doubleArray = new double[stringArray.Length];

            // Преобразование каждой строки в число с плавающей точкой
            for (int i = 0; i < stringArray.Length; i++)
            {
                if (double.TryParse(stringArray[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    doubleArray[i] = result;
                }
                else
                {
                    // Обработка ошибки, если преобразование не удалось
                    Console.WriteLine($"Ошибка преобразования строки {stringArray[i]} в число.");
                    // Можно предпринять другие действия по обработке ошибки по вашему усмотрению
                }
            }

            return doubleArray;
        }


        /// <summary>
        /// returns an array of minutes, when the customers arrive during the hour
        /// </summary>
        /// <param name="customersCountForAnHour"></param>
        /// <returns></returns>
        public static int[] RandomCustomerDistribution(int customersCountForAnHour)
        {
            Random random = new Random();
            List<int> values = new List<int>();

            // Генерируем случайные значения
            for (int i = 0; i < customersCountForAnHour; i++)
            {
                var newValue = random.Next(0, 60);
                //while (values.Contains(newValue))
                //{
                //    newValue = random.Next(0, 60);
                //}
                values.Add(newValue);
            }

            return values.Order().ToArray();
        }
    }
}
