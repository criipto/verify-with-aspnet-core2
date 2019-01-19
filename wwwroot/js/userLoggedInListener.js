var eventMethod = window.addEventListener ? "addEventListener" : "attachEvent";
var eventer = window[eventMethod];
var messageEvent = eventMethod == "attachEvent" ? "onmessage" : "message";

// Listen to message from child window and send the user to the desired target URL. 
// For this demo, we go to the UserClaims action on the Home controller, but you could certainly add some more
// refined logic for taking the user to a better place.
eventer(messageEvent, function (e) {
  var origin =
    document.location.origin ||
    document.location.protocol + "//" + document.location.host;

  if (e && e.data && e.origin === origin && e.data.userLoggedIn) {
    window.location = '/Home/UserClaims';
  }
}, false);
