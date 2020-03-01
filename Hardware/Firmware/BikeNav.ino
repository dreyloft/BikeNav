/*
   (c)2015 - 2016 Written by Steffen Roth
*/

#include <SPI.h>                                   // SPI Bus lib required by display
#include <Wire.h>                                  // req by display
#include <Adafruit_GFX.h>                          // req by display stores symbols, chars, simple forms
#include <Adafruit_SSD1306.h>                      // req by display general display driver for Adafruit based OLED displays

#define PHOTOSENSORPIN 0                           // Photocell as brightness sensor
#define OLED_RESET 4                               // required by adafruits lib
#define TIMEOUTTIME 5                              // max time without valid Bluetooth signal is seconds

Adafruit_SSD1306 display(OLED_RESET);              // definition and init of the OLED display do not change

const bool ShiftPWM_invertOutputs = false;         // false = LEDs turn on if high is on output
const bool ShiftPWM_balanceLoad = true;            // saves energy

const int ShiftPWM_latchPin = 8;                   // pin definition for 74HC595 shift register usgae with PWM, data pin = 11, clk pin = 13

#include <ShiftPWM.h>                              // have to be included after setting up pins

unsigned char maxBrightness = 255;                 // max brightness of the LEDs
unsigned char pwmFrequency = 75;                   // PWM frequency has influence to reaction time on hole program

unsigned int numRegisters = 2;                     // number of 8-bit shift registers type 74HC595
unsigned int fadingMode = 0;                       // all LEDs starting off
unsigned int displaySwitch = 0;                    // for switching between speed and distance on display
unsigned int unit = 0;                             // unit of length
unsigned int unitBorderValue = 0;                  // border in Unit calculation to switch to bigger value (m -> km, yd -> mi, ft -> mi)
unsigned int maximumDisplayedValue = 0;            // maximum of displayed value in unit calculation
unsigned int unitConvertValue = 0;                 // factor to convert from smaller to bigger value in Unit (m -> km, yd -> mi, ft -> mi)
 
unsigned long startTime = 0;

byte direct = 0;                                   // direction LED on ring
byte timeout = 0;                                  // timeout for Bluetooth connection
byte ledBrightness = 0;                            // LED brightness relative to brightness outside

int photosensorReading = 0;                        // photosensor to register brightness and avoid blending / no visible light signal problems

float displayValue = 0;                            // stores the value for ditance in meters or speed in km/h, float because of big values
float distanceTemp = 0;                            // distance Temp for km calculation see below
float unitFactor = 0;                              // calculaton factor between meter, yards and feet

void setup() {
  ShiftPWM.SetAmountOfRegisters(numRegisters);     // init shift pwm lib / shift registers
  ShiftPWM.Start(pwmFrequency, maxBrightness);
  ShiftPWM.SetAll(0);                              //set all LEDs off

  Serial.begin(9600);                              // init serial connection needed for bluetooth communication
  display.begin(SSD1306_SWITCHCAPVCC, 0x3C);       // init display

  clearDisplay();                                  // display setup
  display.println("welcome to");                   // welcome message
  display.println("BikeNav");
  display.display();
}

void loop() {
  photosensorReading = analogRead(PHOTOSENSORPIN); // read the photosensor voltage to get brightnes indicator
  ledBrightness = 0.063 * photosensorReading + 6.2;// calculation of the brightness 10 = minimum in darkness, 60 = maximum in sunshine

  if (Serial.available() > 0 && Serial.find('d')) {// if bluetooth is available && char d (data) is found
    displaySwitch = Serial.parseInt();             // parse before comma
    direct = Serial.parseInt();                    // parse after comma
    unit = Serial.parseInt();                      // get used unit option
    displayValue = Serial.parseFloat();                // parse after comma

    timeout = 0;                                   // Bluetooth available so timeout = invalid
    clearDisplay();                                // clear display

    if (displayValue == -1) {                      // show destination arrived message if distance = -1, becasue normally negative values are impossible
      display.println("you have");
      display.println("arrived");
      messageFrame();
    } else if (displayValue == -2) {               // show GPS error if no valid position available
      display.println("no GPS");
      display.println("position");
      messageFrame();
    } else {
      if (unit == 0) {                             // if unit is m / km
        unitFactor = 1;                            // input values multiply factor from meters to meters
        unitBorderValue = 1000;                    // switch at 1000 m to 1 km
        unitConvertValue = 1000;                   // get km from m
      } else if (unit == 1) {                      // if unit is yd / mi
        unitFactor = 1.09361;                      // input values multiply factor from meters to yards
        unitBorderValue = 900;                     // switch at 900 yd to 0.5 mi
        unitConvertValue = 1760;                   // get mi from yd
      } else {                                     // if unit is ft / mi
        unitFactor = 3.28084;                      // input values multiply factor from meters to feet
        unitBorderValue = 800;                     // switch at 800 yd to 0.2 mi
        unitConvertValue = 5280;                   // get mi from ft
      }
      
      if (displaySwitch == 0) {                    // if display should show distance between Bike and waypoint
                                                   // if value is lower than 100 km or mi, show value in basic unit
        if (displayValue * unitFactor < unitBorderValue) {
          display.println(displayValue * unitFactor, 0); 
          
          if (unit == 0) {                         // decision of unit
            display.println("m"); 
          } else if (unit == 1) {
            display.println("yd");
          } else {
            display.println("ft");
          }          
        } else {                                   // calculate in next bigger units (m -> km, yd -> mi, ft -> mi)
          distanceTemp = (displayValue * unitFactor) / unitConvertValue;
          
          if (distanceTemp >= 100000  || displayValue < 0) {
            display.println("------");             // if value is bigger than 100k Miles / km check if value is too big for realistic calculation (> 2.5x equator length)
          } else {
            if (distanceTemp < 100) {              // if distance is lower 100 km / mi show value with comma
              display.println(distanceTemp, 1);  
            } else {
              display.println(distanceTemp, 0);    // if value is bigger 100 km / mi show value without comma
            }
          }          
          if (unit == 0) {                         // decision of unit
            display.println("km");  
          } else {
            display.println("mi");
          }
        }
      } else {                                     // if distance will not be shown the actual bike speed have to be shown
        display.println(((displayValue * unitFactor) / unitConvertValue) * 1000, 0);
        if (unit == 0) {
          display.println("km/h");                 // decision of unit
        } else {
          display.println("mph");
        }
      }
      ShiftPWM.SetAll(0);                          // set all LEDs off
      ShiftPWM.SetOne(direct, ledBrightness);      // set correct one on
    }
    display.display();                             //  show all content

    delay(250);                                    // delay to save energy and making the system more "quiet"
  } else {                                         // if bluetooth connection is not available
    if (timeout <= TIMEOUTTIME * 1000 / 100) {     // check if timeout is already reache, if not the old data should be shown, second devide value have to be the same value like in delay time below
      timeout++;
      delay(100);                                  // wait 0.1 sec so new pairing is 2 times per second possible
    } else {                                       // if timeout is reached show that signal is missing
      clearDisplay();

      display.println("bluetooth");                // display message that bluetooth is missing
      display.print(".");
      display.display();

      progressbar(ledBrightness);                  // "progressbar" rising

      display.print(".");                          // dot animation
      display.display();

      progressbar(0);                              // "progressbar" shrinking

      display.print(".");                          // dot animation
      display.display();

      delay(400);                                  // delay to save energy, makeing dot animation more beautiful and making the system more "quiet" again
    }
  }
}

void clearDisplay() {
  display.clearDisplay();                          // deletes all content from OLED Display
  display.setTextSize(2);                          // set Textsize to biggest possible charsize for 2 lines on display
  display.setTextColor(WHITE);                     // color on monochrome display is always white
  display.setCursor(0, 0);                         // sets cursor in upper left corner
}

void progressbar(int brightness) {                 // progressbar is always couting from, LED 0 to 12 and on opposite site from, LED 23 - 13 to take only half delay time
  for (unsigned int i = 0; i < numRegisters * 8 / 2; i++) {
    ShiftPWM.SetOne(i, brightness);
    ShiftPWM.SetOne(numRegisters * 8 - i - 1, brightness);
    delay(50);
  }
}

void messageFrame() {                              // message frame is only every second pixel on
  ShiftPWM.SetAll(0);
  for (unsigned int i = 0; i < numRegisters * 8; i++) {
    if (i % 2 == 0) {
      ShiftPWM.SetOne(i, ledBrightness);
    }
  }
}

