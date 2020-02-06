// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var testUA = function (pattern) {
  return pattern.test(navigator.userAgent);
};
var isiOS = function () {
  return testUA(/iPad|iPhone|iPod/) && !window.MSStream;
};
var isiOSSafari = function () {
  return isiOS() && !testUA(/ CriOS\/[.0-9]*/);
};
var isiOSChrome = function () {
  return isiOS() && testUA(/ CriOS\/[.0-9]*/);
};
var isAndroid = function () { return testUA(/Android/); };
var isWindowsPhone = function () { return testUA(/Windows Phone/i); };
var isWindowsPhone8 = function () { return testUA(/Windows Phone 8/i); };

var pathWithAcrValue = function (path, acrValue, uiLocale) {
  return path + "?acrValue=" + acrValue + '&uiLocale=' + uiLocale; 
};

var framedStrategy = function (acrValue, uiLocale) {
  var selectFrameClass = function (acrValue) {
    return (acrValue || "").replace("urn:grn:authn:", "").replace(/:/g, "-");
  };

  return function showFrame (loginPath) {
    var frame = document.getElementById('verify');
    frame.src = pathWithAcrValue(loginPath, acrValue, uiLocale);
    var frameClass = selectFrameClass(acrValue);
    frame.classList.add('login-frame-' + frameClass);
    frame.classList.add('visible-frame');
    frame.classList.remove('hidden-frame');
  };
};

var redirectStrategy = function (acrValue, uiLocale) {
  return function redirect(loginPath) {
    document.location = pathWithAcrValue(loginPath, acrValue, uiLocale);
  };
};

var selectStrategy = function (acrValue, loginPath, languageRadioName) {
  let uiLocale = $("input[name='" + languageRadioName + "']:checked").val();
  let redirect = redirectStrategy(acrValue, uiLocale);
  let framed = framedStrategy(acrValue, uiLocale);

  if (acrValue === 'urn:grn:authn:se:bankid:same-device') {
    if (isWindowsPhone()) {
      // WinPhone 8 UA string contains 'Android', so handle it first
      if (isWindowsPhone8()) {
        // The reason to use redirect here is because WP8 mis-interprets
        // the X-Frame-Options ALLOW-FROM header that Criipto Verify sends.
        console.log('Same-device SE bankid on WinPhone8 detected. Redirecting.');
        return redirect(loginPath);
      } else {
        console.log('Same-device SE bankid on WinPhone detected. Framing.');
        return framed(loginPath);
      }
    } else if (isiOSSafari()) {
      console.log('Same-device SE bankid on iOS Safari detected. Redirecting');
      return redirect(loginPath);
    } else if (isiOSChrome()) {
      console.log('Same-device SE bankid on iOS Chrome detected. Redirecting');
      return redirect(loginPath);
    } else if (isAndroid()) {
      console.log('Same-device SE bankid on Android detected. Redirecting');
      return redirect(loginPath);
    }
    return framed(loginPath);
  } else if (
      acrValue === 'urn:grn:authn:fi:tupas'
      || acrValue === 'urn:grn:authn:fi:mobileid'
      || acrValue === 'urn:grn:authn:fi:all'
      || acrValue === 'urn:grn:authn:no:bankid'
  ) {
    return redirect(loginPath);
  }

  return framed(loginPath);
};