var a, b, c, angle, v, ledPin;                                                                   //a, b, c for triangle calculation, angle is angel between Actual GPS position and way point (WP), v = velocity, ledPin = register pin which should be high after calculation
var R      = 6378137;                                                                            // Radius of Earth
var noLeds = 16;                                                                                 // Number of LEDs in Circle
var dTime  = 1;                                                                                  // frequency of calculating and sending data (in sec)
var gpsOld = [0,0];                                                                              // last received GPS position
var gpsNew = [0,0];                                                                              // actual GPS position
var gpsWP  = [0,0];                                                                              // PGS position of the way point

function calculation () {
  gpsWP[0] = document.getElementById('wplon').value;                                             // GPS position of the way point
  gpsWP[1] = document.getElementById('wplat').value;
  
  gpsOld = gpsNew.slice(0);                                                                      // GPS position where bike driver was at last calculation
  gpsNew[0] = document.getElementById('lon').value;                                              // GPS position where bike driver is right now
  gpsNew[1] = document.getElementById('lat').value;

  a = getDistance(gpsNew, gpsOld);                                                               // To get the angle the calculation of all three sides of the triangle is needed 
  b = getDistance(gpsOld, gpsWP);
  c = getDistance(gpsWP, gpsNew);

  angle = Math.round(Math.acos((b * b - c * c - a * a) / (-2 * c * a)) * (180 / Math.PI));       // cosine rule to calculate the angle between bike pos and WP pos with three given sides
  v = ((a / dTime) * 3.6).toFixed(1);                                                            // calculation of the speed

  if (!isNaN(angle)) {                                                                           // if angle is not valid (this happens eg. if the bike is not moving) the same led like after the last calculation should just be switched on again
    ledPin = Math.round(angle / (360 / noLeds));
  }

  document.getElementById('nLat').value = gpsNew[0];                                             // debug only
  document.getElementById('nLon').value = gpsNew[1];
  document.getElementById('oLat').value = gpsOld[0];
  document.getElementById('oLon').value = gpsOld[1];

  document.getElementById('dist').value = c;                                                     // the important values
  document.getElementById('angle').value = angle;
  document.getElementById('speed').value = v;
  document.getElementById('led').value = ledPin;
}

function getDistance (p1, p2) {                                                                  // calculation of distances between 2 point on Earth with correct formula to correct "ball specific" errors
  var dLat = rad(p2[1] - p1[1]);
  var dLon = rad(p2[0] - p1[0]);
  var d = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.cos(rad(p1[1])) * Math.cos(rad(p2[1])) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
  return Math.round(R * (2 * Math.atan2(Math.sqrt(d), Math.sqrt(1 - d))));
}

function rad (x) {                                                                               // radiant calculation to get correct angles 
  return x * Math.PI / 180;
}
