/*
   d<a>,<b>,<c>,<d>

   <a>
   selection of display mode on sub screen
   0 = distance
   1 = speed

   <b>
   direction on main screen
   Possible Directions: 0 - 15
   Examples:
   0 = behind
   4 = left
   8 = in front
   12 = right

   <c>
   shown units on sub screen
   0 ... 2
   0 = metrical units (Distances: 1.0 km > 999 m)
   1 = imperial untis (Distances: 1.0 mi > 0.5 mi > 900 yd)
   2 = us units (Distances: 1.0 mi > 0.2 mi > 800 ft)

   <d>
   float value
   value for sub screen (distance or speed) always im metrical units conversation will be done by BikeNav Device(!)
   Distances or speed (legal values): 0 - 10000000 m (10k km)
   Arrival Message: -1
   GPS error Message: -2
*/

using BikeNav.Resources;
using System;
using System.Windows;
using System.Text;
using System.Collections.Generic;
using System.Device.Location;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BikeNav
{
    public partial class MainPage : PhoneApplicationPage
    {
        private bool EmulatorMode = false;                                      // for giving possibility to use BikeNav application also without the Bluetooth device (eg. on emulator)
        private bool BluetoothConnectionActive = false;                         // to prevent error by sending data to Bluetooth device before it is successfully connected     
        private bool ConfirmNewWayPoint = false;
        private bool NewWaypointSet = false;
        private short Cycles;                                                   // counts the executed cycles of angle calculations for enhanced results
        private short NumberOfLeds = 16;                                        // number of used LEDs on circle display
        private short ActiveLedPin;                                             // represents the active LED on cricle display
        private float CurrentAngle;                                             // current angel between direction of bike driver and way point 
        private float ApproximatedAngle;                                        // angel between direction of bike (approximation) 
        private double a, b, c;                                                 // for triangle calculations / distances
        private double Speed;                                                   // speed of bike
        private double DistanceToWayPoint;                                      // distance to next way point in meter
        private float[] ApproxAngle = new float[4];                             // array of calculated angles between bike and way point (needed for approximation)
        private GeoCoordinate GpsCoordinatesCBP;                                // GPS coordinates of C-urrent B-ike P-osition
        private GeoCoordinate GpsCoordinatesCurrentWayPoint;                    // GPS coordinates of current way point
        private StreamSocket BluetoothSocket;                                   // Socket used to communicate with the Bluetooth module
        private List<double[]> ActiveRoute = new List<double[]>();
        private List<double[]> NewRoute = new List<double[]>();
        private int EditWayPointOption = -1;
        private int activeWaypointIndex;
        private int borderValueDistance = 50;
        private int AdditionalInfos = 0;

        Geolocator locator = null;
        private double[] TempGeoPosition = new double[3];

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            TextBox_StatusMessage.Text = "Started in Emulator Mode";       // Status = emulator Mode 
            ApplicationBarOrganizer(-1);

            ActiveRoute.Insert(0, new double[] { 52.443292, 13.386316 });

            // Test if Environment is Emulator and message output if it is start applicator in Emulator mode with missing Bluetooth features
            if (Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator || EmulatorMode == true)
            {
                MessageBox.Show("For full Bluetooth support this application has to be executed on a phone. The Application is running now in feature limited Emulator Mode", "Warning: Emulator mode", MessageBoxButton.OK);
                EmulatorMode = true;

                TextBox_StatusMessage.Text = "Started in Emulator Mode";       // Status = emulator Mode 

                ListBox_PairedBluetoothDevices.Visibility = Visibility.Collapsed;// Quick and Dirty UI change

                TextBox_Latitude.Visibility = Visibility.Visible;
                TextBox_Longitude.Visibility = Visibility.Visible;
                TextBox_Angle.Visibility = Visibility.Visible;
                TextBox_ApproxAngle.Visibility = Visibility.Visible;
                TextBox_Direction.Visibility = Visibility.Visible;
                TextBox_Speed.Visibility = Visibility.Visible;
                TextBox_Distance.Visibility = Visibility.Visible;
                TextBox_WPLong.Visibility = Visibility.Visible;
                TextBox_NoWPs.Visibility = Visibility.Visible;
                ListBox_WayPoints.Visibility = Visibility.Visible;

                Button_ConnectToBikeNav.Visibility = Visibility.Collapsed;
                Button_SendData.Visibility = Visibility.Collapsed;              // end of UI change
            }

            Loaded += MainPage_Loaded;                                          // call of main functionality, extra function becasue of needed async support
        }

        // 'real' main function
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //Geolocator locator = new Geolocator();                              // init geolocator for GPS functionality

            locator = new Geolocator();
            locator.MovementThreshold = 10;
            locator.StatusChanged += geolocator_StatusChanged;

            if (EmulatorMode == false)                                          // Turn off all Bluetooth features if running on emulator
            {
                try                                                             // search and selection of all paired Bluetooth devices
                {
                    PeerFinder.Start();
                    PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";    // find / get All Paired BT Devices
                    var peers = await PeerFinder.FindAllPeersAsync();           // make peers the container for all paired Bluetooth devices
                    TextBox_StatusMessage.Text = "Finding Paired Devices...";  // update status message

                    for (int i = 0; i < peers.Count; i++)                       // add all found Bluetooth devices to list box by adding the name
                    {
                        ListBox_PairedBluetoothDevices.Items.Add(peers[i].DisplayName);
                    }
                    if (peers.Count <= 2)                                       // set / update status message incl. number of found Bluetooth devices
                    {
                        TextBox_StatusMessage.Text = "Found " + peers.Count + " devices, please select the BikeNav device";
                    }
                }
                catch (Exception ex)                                            // if error 
                {
                    // in case of an error check if Bluetooth and location settings are correctly turned on if both 'modules' are deactivated show a message for both errors
                    if ((uint)ex.HResult == 0x8007048F && locator.LocationStatus == PositionStatus.Disabled)
                    {
                        MessageBox.Show("You have to enable your Bluetooth and Location settings");
                    }
                    else if ((uint)ex.HResult == 0x8007048F)                    // if Bluetooth deactivated but location is correctly turned on show message for Bluetooth only
                    {
                        MessageBox.Show("You have to enable your Bluetooth settings");
                    }
                    else if (locator.LocationStatus == PositionStatus.Disabled) // if emulator mode enabled and location deactivated show message for Location
                    {
                        MessageBox.Show("You have to enable your Location settings");
                    }
                }
            }

            if (locator.LocationStatus == PositionStatus.Disabled && EmulatorMode) // if emulator mode enabled and location deactivated show message for Location
            {
                MessageBox.Show("You have to enable your Location settings");
            }

            if (locator.LocationStatus != PositionStatus.Disabled)              // if Bluetooth and Location settings are correct
            {
                GetCoordinate();                                                // search for current GPS position
            }
        }

        // Bluetooth send functionality
        private async void Bluetooth_SendData(string SendData)
        {
            if (BluetoothSocket == null)                                        // error if no device was ever peared to the phone show error message
            {
                MessageBox.Show("You have to pear the BikeNav device in your Bluetooth settings once before it is possible to use here.");
                TextBox_StatusMessage.Text = "No Bluetooth device found. Please check your Bluetooth settings!";
                return;
            }
            else if (BluetoothSocket != null)                                   // if the BikeNav device is peared and successfully connected to the application 
            {
                BluetoothConnectionActive = true;
                var DataBuffer = GetBufferFromByteArray(UTF8Encoding.UTF8.GetBytes(SendData));  // send the data string to device using a buffer
                await BluetoothSocket.OutputStream.WriteAsync(DataBuffer);      // wait until sending is finished
                TextBox_StatusMessage.Text = "Sent data: " + SendData;         // update the status message
            }
        }

        // Bluetooth buffer function, required for async sending process
        private IBuffer GetBufferFromByteArray(byte[] package)
        {
            using (DataWriter dw = new DataWriter())
            {
                dw.WriteBytes(package);
                return dw.DetachBuffer();
            }
        }

        // function for selecting the BikeNav device from list of all peared / found Bluetooth devices in the phone settings
        private void ListBox_PairedBluetoothDevices_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (ListBox_PairedBluetoothDevices.SelectedItem == null)            // if no device becomes selected by tap show an error message
            {
                Button_ConnectToBikeNav.IsEnabled = false;                      // block the connect button to prevent connection errors / application crashes
                TextBox_StatusMessage.Text = "No Device Selected! Try again...";
                return;
            }
            else if (ListBox_PairedBluetoothDevices.SelectedItem != null)       // if a device from list is selected enable the connect button
            {
                Button_ConnectToBikeNav.IsEnabled = true;                       // if a wrong device becomes selected and connect button is pressed the application maybe crashes at this point
            }
        }

        // send button functionality (for development only becomes removed later)
        private void btnSendCommand_Click(object sender, RoutedEventArgs e)
        {
            Bluetooth_SendData("d0," + ActiveLedPin + ",0," + DistanceToWayPoint);// if the user manually requests sending an update of the Data to the BikeNav device
        }

        // send data function for automated data send to the Bikenav device
        private void sendData()
        {
            if (BluetoothConnectionActive)
            {
                Bluetooth_SendData("d0," + ActiveLedPin + ",0," + DistanceToWayPoint);// options are atm missing
                return;
            }
        }

        // funtion for 'reading' GPS data from phone
        private void GetCoordinate()
        {
            var watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High)
            {
                MovementThreshold = 1                                           // 'Listens' for chanegs with high sensitivity
            };
            watcher.PositionChanged += this.watcher_PositionChanged;            //
            watcher.Start();
        }

        // listens for positon changes, if position changed new distances etc becomes calculated and send to BikeNav device
        private void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            var pos = e.Position.Location;
            TextBox_Latitude.Text = "CB Latitude:  " + pos.Latitude.ToString();  // Debug output only
            TextBox_Longitude.Text = "CB Longitude: " + pos.Longitude.ToString(); //
            Speed = pos.Speed;

            GPS_Calculations(pos.Latitude, pos.Longitude);                      // call calculation function
        }

        // calculation funtion uses triange calculation for distances 
        private void GPS_Calculations(double Latitude, double Longitude)
        {
            //double[] GpsCoordinatesCurrentWayPoint = { 52.443292, 13.386316 };         // dummy way point near subway station westphalweg (Berlin)
            GpsCoordinatesCurrentWayPoint = routeManager(activeWaypointIndex);

            GeoCoordinate GpsCoordinatesOld = GpsCoordinatesCBP;                              // set Gps coordinates to array for calculation by difference between new and old position
            GpsCoordinatesCBP = new GeoCoordinate(Latitude, Longitude);         // setup of new position
            
            if (GpsCoordinatesCBP != null && GpsCoordinatesOld != null && GpsCoordinatesCurrentWayPoint != null)
            {
                a = GpsCoordinatesCBP.GetDistanceTo(GpsCoordinatesOld);         // To get the angle the calculation of all three sides of the triangle is needed
                b = GpsCoordinatesOld.GetDistanceTo(GpsCoordinatesCurrentWayPoint);
                c = GpsCoordinatesCurrentWayPoint.GetDistanceTo(GpsCoordinatesCBP);
            }

            DistanceToWayPoint = c;                                             // c = distance between bike and waypoint (Pythagoras)
            CurrentAngle = (float)Math.Round(Math.Acos((b * b - c * c - a * a) / (-2 * c * a)) * (180 / Math.PI)); // cosine rule to calculate the angle between bike pos and WP pos with three given sides

            for (int i = (Cycles - 1); i > 0; i--)                              // comparing to 'number of already done cycles' to be able to deliver an angle from the first time of getting a valid GPS Signal
            {
                ApproxAngle[i] = ApproxAngle[i - 1];                            // 'delete' oldest angle
            }
            ApproxAngle[0] = CurrentAngle;                                      // add newest angle to array as first element

            ApproximatedAngle = 0;

            if (Cycles > 0)                                                     // to prevent error by division through zero
            {
                for (int i = 0; i < Cycles; i++)                                // summarising all valid angle values
                {
                    ApproximatedAngle += ApproxAngle[i];
                }
                ApproximatedAngle = ApproximatedAngle / Cycles;                 // and deviding them trough number of total valid angles
            }
            else
            {
                ApproximatedAngle = CurrentAngle;                               // for first valid GPS signal only
            }

            if (Cycles < (short)ApproxAngle.Length)                             // to prevent from counting issues (overflow)
            {
                Cycles++;
            }
            else
            {
                Cycles = (short)ApproxAngle.Length;                             // not needed but for being on the "safe site"
            }

            if (!float.IsNaN(ApproximatedAngle))                                // if angle is not valid (this happens eg. if the bike is not moving) the same LED like before should be active
            {
                ActiveLedPin = (short)Math.Round(ApproximatedAngle / (360 / NumberOfLeds)); // if angle is valid claculate new active LED
            }

            if (DistanceToWayPoint < borderValueDistance)
            {
                if (ActiveRoute.Count <= activeWaypointIndex)                   // next Waypoint
                {
                    activeWaypointIndex++;
                    GpsCoordinatesCurrentWayPoint = routeManager(activeWaypointIndex);
                }
                else
                {
                    DistanceToWayPoint = -1;                                    // Destination reached message
                }
            }

            TextBox_Angle.Text = "Angle: " + CurrentAngle.ToString() + "°";     // Debug message outputs
            TextBox_ApproxAngle.Text = "ApproxAngle: " + ApproximatedAngle.ToString() + "°";
            TextBox_Direction.Text = "Direction: " + ActiveLedPin.ToString();
            TextBox_Distance.Text = "Distance to WP: " + DistanceToWayPoint.ToString();
            TextBox_Speed.Text = "Speed: " + Speed.ToString();
            TextBox_WPLong.Text = "WP Coordinates:  " + GpsCoordinatesCurrentWayPoint;
            TextBox_NoWPs.Text = "No WPs: " + ActiveRoute.Count;

            if (EmulatorMode == false)
            {
                sendData();                                                     // if everything is calculated successfully send new data to BikeNav device
            }
        }

        // 
        private async void ButtonConnectBikeNav_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_PairedBluetoothDevices.SelectedItem != null)
            {
                PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";        // Grab Paired Devices
                var PF = await PeerFinder.FindAllPeersAsync();                  // Store Paired Devices

                BluetoothSocket = new StreamSocket();                           // Create a new Socket Connection
                await BluetoothSocket.ConnectAsync(PF[ListBox_PairedBluetoothDevices.SelectedIndex].HostName, "1"); // Connect using Socket to Selected Item

                var DataBuffer = GetBufferFromByteArray(Encoding.UTF8.GetBytes("")); // Create Buffer/Packet for Sending
                await BluetoothSocket.OutputStream.WriteAsync(DataBuffer);      // Send Arduino Buffer/Packet Message

                TextBox_StatusMessage.Text = "Connection successfully established"; // success message for Bluetooth connection

                ListBox_PairedBluetoothDevices.Visibility = Visibility.Collapsed; // Quick and Dirty UI change between Bluetooth connect and already connected 'screen'

                TextBox_Latitude.Visibility = Visibility.Visible;
                TextBox_Longitude.Visibility = Visibility.Visible;
                TextBox_Angle.Visibility = Visibility.Visible;
                TextBox_Direction.Visibility = Visibility.Visible;
                TextBox_Speed.Visibility = Visibility.Visible;
                TextBox_Distance.Visibility = Visibility.Visible;
                TextBox_WPLong.Visibility = Visibility.Visible;
                TextBox_NoWPs.Visibility = Visibility.Visible;
                ListBox_WayPoints.Visibility = Visibility.Visible;

                Button_ConnectToBikeNav.Visibility = Visibility.Collapsed;
                Button_SendData.Visibility = Visibility.Visible;                // end of UI change
            }
        }
        
        private GeoCoordinate routeManager(int activeWaypointIndex)
        {
            GpsCoordinatesCurrentWayPoint = new GeoCoordinate(ActiveRoute[activeWaypointIndex][0], ActiveRoute[activeWaypointIndex][1]);

            return GpsCoordinatesCurrentWayPoint;
        }

        void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            switch (args.Status)
            {
                case PositionStatus.Initializing:
                    // the geolocator started the tracking operation
                    TextBox_StatusMessage.Text = "GPS - Initializing";
                    break;
                case PositionStatus.NoData:
                    // the location service was not able to acquire the location
                    TextBox_StatusMessage.Text = "GPS - No Connection to Satellite";
                    DistanceToWayPoint = -2;
                    break;
            }
        }

        private void MapLoaded(object sender, RoutedEventArgs e)
        {
            string appID = "AiVGqbGprot4hWfxbDw2AgrMqb5NgHfJ1XIZtv7r8Az0NLAVXCoVSOI0wpTQqKN6";
            MapsSettings.ApplicationContext.ApplicationId = appID;
            MapsSettings.ApplicationContext.AuthenticationToken = "AiVGqbGprot4hWfxbDw2AgrMqb5NgHfJ1XIZtv7r8Az0NLAVXCoVSOI0wpTQqKN6";
            
            SetMappositionOnStart();
        }

        private void ApplicationBarOrganizer(int Option)
        {
            if (Option != EditWayPointOption)
            {
                EditWayPointOption = Option;

                ApplicationBar = new ApplicationBar();
                ApplicationBar.Opacity = 0.5;

                switch (Option)
                {
                    case 1:
                        NewWayPointApplicationBar();
                        break;
                    case 2:
                        EditWayPointApplicationBar();
                        break;
                    default:
                        break;
                }
            }
        }

        private void NewWayPointApplicationBar()
        {
            ApplicationBarIconButton appBarButton = new ApplicationBarIconButton();

            // Accept button.
            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/check.png", UriKind.Relative));
            appBarButton.Text = "Accept";
            appBarButton.Click += AcceptWayPoint;
            ApplicationBar.Buttons.Add(appBarButton);
            // Cancel button.
            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/cancel.png", UriKind.Relative));
            appBarButton.Text = "Cancel";
            appBarButton.Click += CancelWayPoint;
            ApplicationBar.Buttons.Add(appBarButton);
        }

        // Create the localized ApplicationBar.
        private void EditWayPointApplicationBar()
        {
            ApplicationBarIconButton appBarButton = new ApplicationBarIconButton();

            // Cancel button.
            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/delete.png", UriKind.Relative));
            appBarButton.Text = "Delete";
            appBarButton.Click += CancelWayPoint;
            ApplicationBar.Buttons.Add(appBarButton);
            //Down Button
            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/down.png", UriKind.Relative));
            appBarButton.Text = "Down";
            appBarButton.Click += ChangeWayPointOrderDown;
            ApplicationBar.Buttons.Add(appBarButton);
            //Up Button
            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/up.png", UriKind.Relative));
            appBarButton.Text = "Up";
            appBarButton.Click += ChangeWayPointOrderUp;
            ApplicationBar.Buttons.Add(appBarButton);
        }

        void AcceptWayPoint(object sender, EventArgs e)
        {
            ApplicationBarOrganizer(-1);

            if (NewWaypointSet)
            {
                NewWaypointSet = false;
                ConfirmNewWayPoint = true;
                AddWayPointToRoute(TempGeoPosition[0], TempGeoPosition[1], TempGeoPosition[2]);
                CreateRouteOnMap();
            }
        }

        private void CancelWayPoint(object sender, EventArgs e)
        {
            if (!ConfirmNewWayPoint && NewWaypointSet)
            {
                CreateWayPointsOnMap();
            }
            else if (ListBox_WayPoints.SelectedIndex != -1)
            {
                NewRoute.RemoveAt(ListBox_WayPoints.SelectedIndex);
                RecreateRouteList();
                CreateWayPointsOnMap();
            }

            NewWaypointSet = false;

            ApplicationBarOrganizer(-1);
        }

        private void ChangeWayPointOrderUp(object sender, EventArgs e)
        {
            int selection = ListBox_WayPoints.SelectedIndex;

            if (selection != 0)
            {
                double[] TempData = NewRoute[selection];
                NewRoute[selection] = NewRoute[selection - 1];
                NewRoute[selection - 1] = TempData;

                ListBox_WayPoints.Items.RemoveAt(selection);
                ListBox_WayPoints.Items.RemoveAt(selection - 1);
                ListBox_WayPoints.Items.Insert(selection - 1, selection - 1 + ") " + string.Join(", ", NewRoute[selection - 1]));
                ListBox_WayPoints.Items.Insert(selection, selection + ") " + string.Join(", ", NewRoute[selection]));

                RecreateRouteList();
                CreateWayPointsOnMap();

                ListBox_WayPoints.Focus();
                ListBox_WayPoints.SelectedIndex = selection - 1;
            }
            else
            {
                TextBox_StatusMessage.Text = "Unable To Shift Selected Way Point";
            }
        }

        private void ChangeWayPointOrderDown(object sender, EventArgs e)
        {
            int selection = ListBox_WayPoints.SelectedIndex;

            if (selection != ListBox_WayPoints.Items.Count - 1)
            {
                double[] TempData = NewRoute[selection];
                NewRoute[selection] = NewRoute[selection + 1];
                NewRoute[selection + 1] = TempData;

                ListBox_WayPoints.Items.RemoveAt(selection + 1);
                ListBox_WayPoints.Items.RemoveAt(selection);
                ListBox_WayPoints.Items.Insert(selection, selection + ") " + string.Join(", ", NewRoute[selection]));
                ListBox_WayPoints.Items.Insert(selection + 1, selection + 1 + ") " + string.Join(", ", NewRoute[selection + 1]));

                RecreateRouteList();
                CreateWayPointsOnMap();

                ListBox_WayPoints.Focus();
                ListBox_WayPoints.SelectedIndex = selection + 1;
            }
            else
            {
                TextBox_StatusMessage.Text = "Unable To Shift Selected Way Point";
            }
        }

        private void AddWayPointToRoute(double Latitude, double Longitude, double AdditonalInfo)
        {
            double[] WayPointData = new double[3];
            WayPointData[0] = Latitude;
            WayPointData[1] = Longitude;
            WayPointData[2] = AdditonalInfo;

            NewRoute.Add(WayPointData);
            ApplicationBarOrganizer(-1);
            RecreateRouteList();
        }

        private void RemovePointFromRoute()
        {
            NewRoute.RemoveAt(ListBox_WayPoints.SelectedIndex);
            RecreateRouteList();
            CreateWayPointsOnMap();
            ApplicationBarOrganizer(-1);
        }

        private void RecreateRouteList()
        {
            ListBox_WayPoints.Items.Clear();

            for (int i = 0; i < NewRoute.Count; i++)
            {
                ListBox_WayPoints.Items.Add((i + 1).ToString() + ") " + string.Join(", ", NewRoute[i]));
            }
        }

        private void CreateWayPointsOnMap()
        {
            Map.Layers.Clear();

            SpecialMapIcons();
            CreateRouteOnMap();

            for (int i = 0; i < NewRoute.Count; i++)
            {
                double[] TempData = new double[2];
                TempData = NewRoute[i];
                CreateWayPoint(new GeoCoordinate(TempData[0], TempData[1]), i);
            }
        }

        private void CreateRouteOnMap()
        {
            Map.MapElements.Clear();

            MapPolyline polyline = new MapPolyline();
            polyline.StrokeColor = Color.FromArgb(255, 0, 100, 0);
            polyline.StrokeThickness = 3;

            for (int i = 0; i < NewRoute.Count; i++)
            {
                double[] TempCoordinate = new double[2];
                TempCoordinate = NewRoute[i];
                polyline.Path.Add(new GeoCoordinate(TempCoordinate[0], TempCoordinate[1]));
            }

            Map.MapElements.Add(polyline);
        }

        private void ListBox_WayPoints_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ApplicationBarOrganizer(2);

            if (ListBox_WayPoints.SelectedItem != null && NewWaypointSet)
            {
                CancelWayPoint(sender, e);
                TempGeoPosition = NewRoute[ListBox_WayPoints.SelectedIndex];
                NewWaypointSet = false;
            }
        }

        private void CheckBox_Landmarks_Checked(object sender, RoutedEventArgs e)
        {
            Map.LandmarksEnabled = true;
        }

        private void CheckBox_Landmarks_Unchecked(object sender, RoutedEventArgs e)
        {
            Map.LandmarksEnabled = false;
        }

        void CreateWayPoint(GeoCoordinate GPSPosition, int WayPointNumber)
        {
            //Creating a Grid element.
            Grid MyGrid = new Grid();
            MyGrid.RowDefinitions.Add(new RowDefinition());
            MyGrid.RowDefinitions.Add(new RowDefinition());
            MyGrid.Background = new SolidColorBrush(Colors.Transparent);

            //Creating the Cricle
            Ellipse Circle = new Ellipse();
            Circle.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 50, 0));
            Circle.Height = 20;
            Circle.Width = 20;
            Circle.SetValue(Grid.RowProperty, 0);
            Circle.SetValue(Grid.ColumnProperty, 0);

            //Adding the Circle to the Grid
            MyGrid.Children.Add(Circle);

            //Creating the Polygon Arrow
            Polygon Arrow = new Polygon();
            Arrow.Points.Add(new Point(5, -2));
            Arrow.Points.Add(new Point(7, 0));
            Arrow.Points.Add(new Point(13, 0));
            Arrow.Points.Add(new Point(15, -2));
            Arrow.Points.Add(new Point(10, 15));
            Arrow.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 100, 0));
            Arrow.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 100, 0));
            Arrow.SetValue(Grid.RowProperty, 1);
            Arrow.SetValue(Grid.ColumnProperty, 0);

            //Adding the Polygon to the Grid
            MyGrid.Children.Add(Arrow);

            // Adding Number of Way Point
            MyGrid.Children.Add(new TextBlock()
            {
                Text = (WayPointNumber + 1).ToString(),
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.White),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            //Creation of the Overlay and set Grid as content
            MapOverlay MyOverlay = new MapOverlay();
            MyOverlay.Content = MyGrid;
            MyOverlay.GeoCoordinate = GPSPosition;
            MyOverlay.PositionOrigin = new Point(0.5, 1);

            //Creating a MapLayer and adding the MapOverlay to it
            MapLayer WayPointLayer = new MapLayer();
            WayPointLayer.Add(MyOverlay);
            Map.Layers.Add(WayPointLayer);

            ConfirmNewWayPoint = false;
        }

        private void Map_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!ConfirmNewWayPoint && NewWaypointSet)
            {
                CreateRouteOnMap();
                CreateWayPointsOnMap();
                SpecialMapIcons();
            }

            ApplicationBarOrganizer(1);

            GeoCoordinate tapLocation = Map.ConvertViewportPointToGeoCoordinate(e.GetPosition(Map));
            CreateWayPoint(tapLocation, ListBox_WayPoints.Items.Count);
            NewWaypointSet = true;

            try
            {
                TempGeoPosition[0] = tapLocation.Latitude;
                TempGeoPosition[1] = tapLocation.Longitude;
                TempGeoPosition[2] = AdditionalInfos;
                TextBox_StatusMessage.Text = "New Position Set";
            }
            catch
            {
                TextBox_StatusMessage.Text = "Invalid Position";
            }
        }

        private void SetMappositionOnStart()
        {
            Map.ZoomLevel = 17;

            // load File action here

            if (GpsCoordinatesCBP == null)
            {
                Map.Center = new GeoCoordinate(52.444406, 13.3901803);
            }
            SpecialMapIcons();
        }

        private void SpecialMapIcons()
        {
            MapLayer layer = new MapLayer();
            List<string[]> SpecialIcons = new List<string[]>();

            SpecialIcons.Add(new string[] { "meelogic.png", "52.50209", "13.44912" });
            SpecialIcons.Add(new string[] { "squirrel-entertainment.png", "52.44429", "13.39212" });
            
            for (int i = 0; i < SpecialIcons.Count; i++)
            {
                string[] Icons = SpecialIcons[i];

                MapOverlay overlay = new MapOverlay()
                {
                    GeoCoordinate = new GeoCoordinate(float.Parse(Icons[1], System.Globalization.CultureInfo.InvariantCulture), 
                                                      float.Parse(Icons[2], System.Globalization.CultureInfo.InvariantCulture)),

                    Content = new Image
                    {
                        Source = new BitmapImage(new Uri("/Assets/icon/" + Icons[0], UriKind.Relative)),
                        Width = 50,
                        Height = 50,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                };
                layer.Add(overlay);
            }
            Map.Layers.Add(layer);
        }
    }
}
