Xcode ("10.2.1")
  .XcodeSelect ();
Item ("Xamarin.Mac", "5.0.0.0")
  .Condition (FxVersionDiffers)
  .Source (xm => $"https://download.visualstudio.microsoft.com/download/pr/3849ffa9-0763-4727-8ae1-d67da9a6e60c/5c02d563ef9568fa79ba6dcf0cfeabd0/xamarin.mac-5.0.0.0.pkg");
Item ("Xamarin.iOS", "12.0.0.15")
  .Condition (FxVersionDiffers)
  .Source (xi => $"https://download.visualstudio.microsoft.com/download/pr/28396824-ecde-429e-9693-0da9382e1474/debd3c099bf5a516b964675c2628e601/xamarin.ios-12.0.0.15.pkg");
Item ("Mono", "5.12.0.309")
  .Condition (FxVersionDiffers)
  .Source (mono => $"https://download.visualstudio.microsoft.com/download/pr/94c4042e-1257-4c72-9028-1fc7024e634e/1725060bf52e70b0a52dc43d40562ef2/monoframework-mdk-5.12.0.309.macos10.xamarin.universal.pkg");
