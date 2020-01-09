// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace SwiftReflector.Importing {
	public partial class TypeAggregator {

		static partial void ModulesToSkipIOS (ref HashSet<string> result) { result = iOSModulesToSkip; }
		static HashSet<string> iOSModulesToSkip = new HashSet<string> () {
			"IdentityLookupUI",
	    		"AuthenticationServices",
			"OpenTK",
	    		"CoreMidi",
			"Registrar",
	    		"Twitter",
			"Network",
	    		"NaturalLanguage",
			"CoreAnimation",
	    		"ClassKit",
			"CoreServices",
	    		"CarPlay",
			"BusinessChat",
	    		"System",
			"System.Drawing",
	    		"Xamarin.Utils",
			"ObjCRuntime",
	    		"VideoSubscriberAccount",
			"IOSurface",
		};

		static partial void AvailableMapIOS (ref Dictionary<string, string> result)
		{
			result = iOSAvailableMap;
		}

		static Dictionary<string, String> iOSAvailableMap = new Dictionary<string, string> () {
		};

		static partial void TypesToSkipIOS (ref HashSet<string> result) { result = iOSTypesToSkip; }
		static HashSet<string> iOSTypesToSkip = new HashSet<string> () {
			// Accounts
			"Accounts.ACFacebookAudience",
			// CoreGraphics
	    		"CoreGraphics.CGColorConverterTransformType",
			"CoreGraphics.CGTextEncoding", // Deprecated
	    		"CoreGraphics.CGImagePixelFormatInfo", // can't find it?
			"CoreGraphics.CGBitmapFlags", // can't find it
	    		"CoreGraphics.CGImageColorModel", // can't find it
			"CoreGraphics.CGColorConverterTriple", // can't find it
	    		"CoreGraphics.GColorConversionInfoTriple", // can't find it
			"CoreGraphics.MatrixOrder",
			// CoreTelephony
	    		"CoreTelephony.CTErrorDomain",
	    		"CoreTelephony.CTCellularPlanProvisioningAddPlanResult",
			// Foundation
			"Foundation.NSFileType",
	    		"Foundation.NSUserDefaultsType",
			"Foundation.NSBundleExecutableArchitecture",
	    		"Foundation.NSNetServicesStatus",
			"Foundation.NSCocoaError",
	    		"Foundation.NSUrlError",
			"Foundation.NSStringDrawingOptions",
	    		"Foundation.NSUbiquitousKeyValueStoreChangeReason",
			"Foundation.NSAlignmentOptions",
	    		"Foundation.NSAttributedStringEnumeration",
			"Foundation.NSUnderlineStyle",
	    		"Foundation.NSWritingDirection",
			"Foundation.NSByteCountFormatterCountStyle",
	    		"Foundation.NSLigatureType",
			"Foundation.NSDateComponentsWrappingBehavior",
	    		"Foundation.NSUrlErrorCancelledReason",
			"Foundation.NSUrlRelationship",
	    		"Foundation.NSStringEncoding",
			"Foundation.NSDocumentType",
	    		"Foundation.NSDocumentViewMode",
			"Foundation.NSItemDownloadingStatus",
	    		"Foundation.NSLinguisticTagUnit",
			"Foundation.NSUrlSessionMultipathServiceType",
	    		"Foundation.NSRunLoopMode",
			"Foundation.NSTextWritingDirection", // deprecated in 9.0
			// HomeKit
			"HomeKit.HMAccessoryCategoryType", // not an enum
			"HomeKit.HMActionSetType", // not an enum
			"HomeKit.HMCharacteristicMetadataFormat", // not an enum
			"HomeKit.HMCharacteristicMetadataUnits", // not an enum
			"HomeKit.HMCharacteristicType", // not an enum
			"HomeKit.HMServiceType", // not an enum
			// IdentityLookup
			"IdentityLookup.ILClassificationAction",
			// IntentsUI
	    		"IntentsUI.INUIAddVoiceShortcutButton",
			"IntentsUI.INUIAddVoiceShortcutButtonStyle",
			// Messages
			"Messages.MSMessagesAppPresentationContext",
			// PassKit
			"PassKit.PKErrorCode", // does not exist
			// SafariServices
			"SafariServices.SFSafariViewControllerDismissButtonStyle",
			// UIKit
			"UIKit.UIAccessibilityPostNotification", // method in swift - not needed.
	    		"UIKit.UIFontDescriptorAttribute",
			"UIKit.NSTextEffect",
	    		"UIKit.UIAccessibilityTrait", // type alias
			"UIKit.UICollectionElementKindSection",
	    		"UIKit.UIDocumentMenuOrder", // deprecation in 11.0
			"UIKit.UILineBreakMode", // deprecated
	    		"UIKit.UIPencilPreferredAction", // 12.1+
			"UIKit.UIRemoteNotificationType", // deprecated in 8
	    		"UIKit.UISegmentedControlStyle", // deprecated
			"UIKit.UITableViewCellAccessory",
	    		"UIKit.UITableViewCellState",
			"UIKit.UITextAlignment", // deprecated
	    		"UIKit.UIToolbarPosition", // doesn't exist?
			"UIKit.UIUserNotificationActionBehavior", // deprecated in 10.0
	    		"UIKit.UIUserNotificationActivationMode", // deprecated in 10.0
			"UIKit.UIUserNotificationType", // deprecated in 10.0
	    		"UIKit.UIGraphicsImageRendererFormatRange", // doesn't exist?
			"UIKit.UIImagePickerControllerImageUrlExportPreset", // can't find it?
	    		"UIKit.UIPrintErrorCode", // can't find it?
			"UIKit.UIPrintError", // can't find it?
			"UIKit.UITransitionViewControllerKind", // can't find it?
	    		"UIKit.UIUserInterfaceStyle", // can't find it?
			"UIKit.UIUserNotificationActionContext", // deprecated
			// VideoToolbox
			"VideoToolbox.VTStatus",
			"VideoToolbox.VTProfileLevel",
			"VideoToolbox.VTH264EntropyMode",
	    		"VideoToolbox.VTFieldCount",
	    		"VideoToolbox.VTFieldDetail",
	    		"VideoToolbox.VTColorPrimaries",
	    		"VideoToolbox.VTFTransferFunction",
	    		"VideoToolbox.VTFieldCount",
			"VideoToolbox.VTYCbCrMatrix",
	    		"VideoToolbox.VTFieldMode",
			"VideoToolbox.VTDeinterlaceMode",
	    		"VideoToolbox.VTOnlyTheseFrames",
			"VideoToolbox.VTPropertyType",
	    		"VideoToolbox.VTReadWriteStatus",
	    		"VideoToolbox.VTScalingMode",
	    		"VideoToolbox.VTDownsamplingMode",
			// Vision
	    		"Vision.VNBarcodeObservationRequestRevision",
			"Vision.VNCoreMLRequestRevision",
	    		"Vision.VNDetectBarcodesRequestRevision",
			"Vision.VNDetectedObjectObservationRequestRevision",
	    		"Vision.VNDetectFaceLandmarksRequestRevision",
			"Vision.VNDetectFaceRectanglesRequestRevision",
	    		"Vision.VNDetectHorizonRequestRevision",
			"Vision.VNDetectRectanglesRequestRevision",
	    		"Vision.VNDetectTextRectanglesRequestRevision",
			"Vision.VNFaceObservationRequestRevision",
	    		"Vision.VNHomographicImageRegistrationRequestRevision",
			"Vision.VNRecognizedObjectObservationRequestRevision",
	    		"Vision.VNRectangleObservationRequestRevision",
			"Vision.VNRequestRevision",
	    		"Vision.VNTextObservationRequestRevision",
			"Vision.VNTranslationalImageRegistrationRequestRevision",
	    		"Vision.VNTrackObjectRequestRevision",
			"Vision.VNTrackRectangleRequestRevision",
		};

		static partial void TypeNamesToMapIOS (ref Dictionary <string, string> result) { result = iOSTypeNamesToMap; }
		static Dictionary<string, string> iOSTypeNamesToMap = new Dictionary<string, string> {
	    		// Foundation
			{ "Foundation.NSBundle", "Bundle" },
			{ "Foundation.NSCalendarType", "NSCalendar.Identifier" },
			{ "Foundation.NSFileProtection", "FileProtectionType" },
			{ "Foundation.NSIndexPath", "IndexPath" },
			{ "Foundation.NSStreamSocketSecurityLevel", "StreamSocketSecurityLevel" },
			{ "Foundation.NSStreamServiceType", "StreamNetworkServiceTypeValue" },
			{ "Foundation.NSUrlCredentialPersistence", "URLCredential.Persistence" },
			{ "Foundation.NSComparisonResult", "ComparisonResult" },
			{ "Foundation.NSUrlRequestCachePolicy", "NSURLRequest.CachePolicy" },
			{ "Foundation.NSUrlCacheStoragePolicy", "URLCache.StoragePolicy" },
			{ "Foundation.NSStreamStatus", "Stream.Status" },
			{ "Foundation.NSPropertyListFormat", "PropertyListSerialization.PropertyListFormat" },
			{ "Foundation.NSPropertyListMutabilityOptions", "PropertyListSerialization.MutabilityOptions" },
			{ "Foundation.NSPropertyListWriteOptions", "PropertyListSerialization.WriteOptions" },
			{ "Foundation.NSPropertyListReadOptions", "PropertyListSerialization.ReadOptions"},
			{ "Foundation.NSMachPortRights", "NSMachPort.Options" },
			{ "Foundation.NSNetServiceOptions", "NetService.Options" },
			{ "Foundation.NSDateFormatterStyle", "DateFormatter.Style" },
			{ "Foundation.NSDateFormatterBehavior", "DateFormatter.Behavior" },
			{ "Foundation.NSHttpCookieAcceptPolicy", "HTTPCookie.AcceptPolicy" },
			{ "Foundation.NSCalendarUnit", "NSCalendar.Unit" },
			{ "Foundation.NSDataReadingOptions", "NSData.ReadingOptions" },
			{ "Foundation.NSDataWritingOptions", "NSData.WritingOptions" },
			{ "Foundation.NSOperationQueuePriority", "Operation.QueuePriority" },
			{ "Foundation.NSNotificationCoalescing", "NotificationQueue.NotificationCoalescing" },
			{ "Foundation.NSPostingStyle", "NotificationQueue.PostingStyle" },
			{ "Foundation.NSDataSearchOptions", "NSData.SearchOptions" },
			{ "Foundation.NSExpressionType", "NSExpression.ExpressionType" },
			{ "Foundation.NSStreamEvent", "Stream.Event" },
			{ "Foundation.NSComparisonPredicateModifier", "NSComparisonPredicate.Modifier" },
			{ "Foundation.NSPredicateOperatorType", "NSComparisonPredicate.Operator" },
			{ "Foundation.NSComparisonPredicateOptions", "NSComparisonPredicate.Options" },
			{ "Foundation.NSCompoundPredicateType", "NSCompoundPredicate.LogicalType" },
			{ "Foundation.NSVolumeEnumerationOptions", "FileManager.VolumeEnumerationOptions" },
			{ "Foundation.NSDirectoryEnumerationOptions", "FileManager.DirectoryEnumerationOptions" },
			{ "Foundation.NSFileManagerItemReplacementOptions", "FileManager.ItemReplacementOptions" },
			{ "Foundation.NSSearchPathDirectory", "FileManager.SearchPathDirectory" },
			{ "Foundation.NSSearchPathDomain", "FileManager.SearchPathDomainMask" },
			{ "Foundation.NSRoundingMode", "NSDecimalNumber.RoundingMode" },
			{ "Foundation.NSCalculationError", "NSDecimalNumber.CalculationError" },
			{ "Foundation.NSNumberFormatterStyle", "NumberFormatter.Style" },
			{ "Foundation.NSNumberFormatterBehavior", "NumberFormatter.Behavior" },
			{ "Foundation.NSNumberFormatterPadPosition", "NumberFormatter.PadPosition" },
			{ "Foundation.NSNumberFormatterRoundingMode", "NumberFormatter.RoundingMode" },
			{ "Foundation.NSFileVersionReplacingOptions", "NSFileVersion.ReplacingOptions" },
			{ "Foundation.NSFileVersionAddingOptions", "NSFileVersion.AddingOptions" },
			{ "Foundation.NSFileCoordinatorReadingOptions", "NSFileCoordinator.ReadingOptions" },
			{ "Foundation.NSFileCoordinatorWritingOptions", "NSFileCoordinator.WritingOptions" },
			{ "Foundation.NSLinguisticTaggerOptions", "NSLinguisticTagger.Options" },
			{ "Foundation.NSJsonReadingOptions", "JSONSerialization.ReadingOptions" },
			{ "Foundation.NSJsonWritingOptions", "JSONSerialization.WritingOptions" },
			{ "Foundation.NSLocaleLanguageDirection", "NSLocale.LanguageDirection"},
			{ "Foundation.NSAlignmentOptions", "NSRect.AlignmentOptions" },
			{ "Foundation.NSFileWrapperReadingOptions", "FileWrapper.ReadingOptions" },
			{ "Foundation.NSFileWrapperWritingOptions", "FileWrapper.WritingOptions" },
			{ "Foundation.NSByteCountFormatterUnits", "ByteCountFormatter.Units" },
			{ "Foundation.NSUrlBookmarkCreationOptions", "NSURL.BookmarkCreationOptions" },
			{ "Foundation.NSUrlBookmarkResolutionOptions", "NSURL.BookmarkResolutionOptions" },
			{ "Foundation.NSUrlRequestNetworkServiceType", "NSURLRequest.NetworkServiceType" },
			{ "Foundation.NSCalendarOptions", "NSCalendar.Options" },
			{ "Foundation.NSDataBase64DecodingOptions", "NSData.Base64DecodingOptions" },
			{ "Foundation.NSDataBase64EncodingOptions", "NSData.Base64EncodingOptions" },
			{ "Foundation.NSUrlSessionAuthChallengeDisposition", "URLSession.AuthChallengeDisposition" },
			{ "Foundation.NSUrlSessionTaskState", "URLSessionTask.State" },
			{ "Foundation.NSUrlSessionResponseDisposition", "URLSession.ResponseDisposition" },
			{ "Foundation.NSActivityOptions", "ProcessInfo.ActivityOptions" },
			{ "Foundation.NSTimeZoneNameStyle", "NSTimeZone.NameStyle" },
			{ "Foundation.NSItemProviderErrorCode", "NSItemProvider.ErrorCode" },
			{ "Foundation.NSDateComponentsFormatterUnitsStyle", "DateComponentsFormatter.UnitsStyle" },
			{ "Foundation.NSDateComponentsFormatterZeroFormattingBehavior", "DateComponentsFormatter.ZeroFormattingBehavior" },
			{ "Foundation.NSFormattingContext", "Formatter.Context" },
			{ "Foundation.NSDateIntervalFormatterStyle", "DateIntervalFormatter.Style" },
			{ "Foundation.NSEnergyFormatterUnit", "EnergyFormatter.Unit" },
			{ "Foundation.NSFormattingUnitStyle", "Formatter.UnitStyle" },
			{ "Foundation.NSMassFormatterUnit", "MassFormatter.Unit" },
			{ "Foundation.NSLengthFormatterUnit", "LengthFormatter.Unit" },
			{ "Foundation.NSQualityOfService", "QualityOfService" },
			{ "Foundation.NSProcessInfoThermalState", "ProcessInfo.ThermalState" },
			{ "Foundation.NSTextCheckingType", "NSTextCheckingResult.CheckingType" },
			{ "Foundation.NSRegularExpressionOptions", "NSRegularExpression.Options" },
			{ "Foundation.NSMatchingOptions", "NSRegularExpression.MatchingOptions" },
			{ "Foundation.NSMatchingFlags", "NSRegularExpression.MatchingFlags" },
			{ "Foundation.NSPersonNameComponentsFormatterOptions", "PersonNameComponentsFormatter.Options" },
			{ "Foundation.NSPersonNameComponentsFormatterStyle", "PersonNameComponentsFormatter.Style" },
			{ "Foundation.NSDecodingFailurePolicy", "NSCoder.DecodingFailurePolicy" },
			{ "Foundation.NSIso8601DateFormatOptions", "ISO8601DateFormatter.Options" },
			{ "Foundation.NSUrlSessionTaskMetricsResourceFetchType", "URLSessionTaskMetrics.ResourceFetchType" },
			{ "Foundation.NSMeasurementFormatterUnitOptions", "MeasurementFormatter.UnitOptions" },
			{ "Foundation.NSUrlSessionDelayedRequestDisposition", "URLSession.DelayedRequestDisposition" },
			{ "Foundation.NSStringCompareOptions", "NSString.CompareOptions" },
			{ "Foundation.NSStringTransform", "StringTransform" },
			{ "Foundation.NSOperatingSystemVersion", "OperatingSystemVersion" },
			{ "Foundation.NSDecimal", "Decimal" },
			// HealthKit
			{ "HealthKit.HKErrorCode", "HKError.Code" },
			{ "HealthKit.HKFhirResourceType", "HKFHIRResourceType" },
			// HomeKit
			{ "HomeKit.HMCharacteristicValueAirParticulate", "HMCharacteristicValueAirParticulateSize" },
			{ "HomeKit.HMCharacteristicValueLockMechanism", "HMCharacteristicValueLockMechanismLastKnownAction" },
			// LocalAuthentication
			{ "LocalAuthentication.LAStatus", "LAError" },
			// PassKit
			{ "PassKit.PKContactFields", "PKContactField" },
			{ "PassKit.PKPassKitErrorCode", "PKPassKitError.Code" },
			{ "PassKit.PKPaymentErrorCode", "PKPaymentError.Code" },
			// SafariServices
			{ "SafariServices.SFErrorCode", "SFError.Code" },
			// StoreKit
			{ "StoreKit.SKProductDiscountPaymentMode", "SKProductDiscount.PaymentMode" },
			{ "StoreKit.SKProductPeriodUnit", "SKProduct.PeriodUnit" },
			// SystemConfiguration
			{ "SystemConfiguration.NetworkReachabilityFlags", "SCNetworkReachabilityFlags" },
			// UIKit
			{ "UIKit.NSControlCharacterAction", "NSLayoutManager.ControlCharacterAction" },
			{ "UIKit.NSGlyphProperty", "NSLayoutManager.GlyphProperty" },
			{ "UIKit.NSLayoutAttribute", "NSLayoutConstraint.Attribute" },
			{ "UIKit.NSLayoutFormatOptions", "NSLayoutConstraint.FormatOptions" },
			{ "UIKit.NSLayoutRelation", "NSLayoutConstraint.Relation" },
			{ "UIKit.NSTextLayoutOrientation", "NSLayoutManager.TextLayoutOrientation" },
			{ "UIKit.NSTextStorageEditActions", "NSTextStorage.EditActions" },
			{ "UIKit.UIAccessibilityCustomRotorDirection", "UIAccessibilityCustomRotor.Direction" },
			{ "UIKit.UIAccessibilityCustomSystemRotorType", "UIAccessibilityCustomRotor.SystemRotorType" },
			{ "UIKit.UIAccessibilityHearingDeviceEar", "UIAccessibility.HearingDeviceEar" },
			{ "UIKit.UIAccessibilityZoomType", "UIAccessibility.ZoomType" },
			{ "UIKit.UIActivityCategory", "UIActivity.Category" },
			{ "UIKit.UIActivityIndicatorViewStyle", "UIActivityIndicatorView.Style" },
			{ "UIKit.UIAlertActionStyle", "UIAlertAction.Style" },
			{ "UIKit.UIAlertControllerStyle", "UIAlertController.Style" },
			{ "UIKit.UIApplicationShortcutIconType", "UIApplicationShortcutIcon.IconType" },
			{ "UIKit.UIApplicationState", "UIApplication.State" },
			{ "UIKit.UIAttachmentBehaviorType", "UIAttachmentBehavior.AttachmentType" },
			{ "UIKit.UIBarButtonItemStyle", "UIBarButtonItem.Style" },
			{ "UIKit.UIBarButtonSystemItem", "UIBarButtonItem.SystemItem" },
			{ "UIKit.UIBlurEffectStyle", "UIBlurEffect.Style" },
			{ "UIKit.UIButtonType", "UIButton.ButtonType" },
			{ "UIKit.UICloudSharingPermissionOptions", "UICloudSharingController.PermissionOptions" },
			{ "UIKit.UICollectionElementCategory", "UICollectionView.ElementCategory" },
			{ "UIKit.UICollectionUpdateAction", "UICollectionViewUpdateItem.Action" },
			{ "UIKit.UICollectionViewCellDragState", "UICollectionViewCell.DragState" },
			{ "UIKit.UICollectionViewDropIntent", "UICollectionViewDropProposal.Intent" },
			{ "UIKit.UICollectionViewFlowLayoutSectionInsetReference", "UICollectionViewFlowLayout.SectionInsetReference" },
			{ "UIKit.UICollectionViewReorderingCadence", "UICollectionView.ReorderingCadence" },
			{ "UIKit.UICollectionViewScrollDirection", "UICollectionView.ScrollDirection" },
			{ "UIKit.UICollectionViewScrollPosition", "UICollectionView.ScrollPosition" },
			{ "UIKit.UICollisionBehaviorMode", "UICollisionBehavior.Mode" },
			{ "UIKit.UIControlContentHorizontalAlignment", "UIControl.ContentHorizontalAlignment" },
			{ "UIKit.UIControlContentVerticalAlignment", "UIControl.ContentVerticalAlignment" },
			{ "UIKit.UIControlEvents", "UIControl.Event" },
			{ "UIKit.UIControlState", "UIControl.State" },
			{ "UIKit.UIDataDetectorType", "UIDataDetectorTypes"},
			{ "UIKit.UIDatePickerMode", "UIDatePicker.Mode" },
			{ "UIKit.UIDeviceBatteryState", "UIDevice.BatteryState" },
			{ "UIKit.UIDocumentBrowserActionAvailability", "UIDocumentBrowserAction.Availability"},
			{ "UIKit.UIDocumentBrowserErrorCode", "UIDocumentBrowserError.Code" },
			{ "UIKit.UIDocumentChangeKind", "UIDocument.ChangeKind" },
			{ "UIKit.UIDocumentSaveOperation", "UIDocument.SaveOperation" },
			{ "UIKit.UIDocumentState", "UIDocument.State" },
			{ "UIKit.UIEventSubtype", "UIEvent.EventSubtype" },
			{ "UIKit.UIEventType", "UIEvent.EventType" },
			{ "UIKit.UIFontDescriptorSymbolicTraits", "UIFontDescriptor.SymbolicTraits" },
			{ "UIKit.UIFontTextStyle", "UIFont.TextStyle" },
			{ "UIKit.UIFontWeight", "UIFont.Weight" },
			{ "UIKit.UIGestureRecognizerState", "UIGestureRecognizer.State" },
			{ "UIKit.UIGuidedAccessRestrictionState", "UIAccessibility.GuidedAccessRestrictionState" },
			{ "UIKit.UIGuidedAccessErrorCode", "UIAccessibility.GuidedAccessError.Code" },
			{ "UIKit.UIImageOrientation", "UIImage.Orientation" },
			{ "UIKit.UIImagePickerControllerCameraCaptureMode", "UIImagePickerController.CameraCaptureMode" },
			{ "UIKit.UIImagePickerControllerCameraDevice", "UIImagePickerController.CameraDevice" },
			{ "UIKit.UIImagePickerControllerCameraFlashMode", "UIImagePickerController.CameraFlashMode" },
			{ "UIKit.UIImagePickerControllerQualityType", "UIImagePickerController.QualityType" },
			{ "UIKit.UIImagePickerControllerSourceType", "UIImagePickerController.SourceType" },
			{ "UIKit.UIImagePickerControllerImageUrlExportPreset", "UIImagePickerController.ImageURLExportPreset" },
			{ "UIKit.UIImageRenderingMode", "UIImage.RenderingMode" },
			{ "UIKit.UIImageResizingMode", "UIImage.ResizingMode" },
			{ "UIKit.UIImpactFeedbackStyle", "UIImpactFeedbackGenerator.FeedbackStyle" },
			{ "UIKit.UIInputViewStyle", "UIInputView.Style" },
			{ "UIKit.UIInterpolatingMotionEffectType", "UIInterpolatingMotionEffect.EffectType" },
			{ "UIKit.UILayoutConstraintAxis", "NSLayoutConstraint.Axis" },
			{ "UIKit.UIMenuControllerArrowDirection", "UIMenuController.ArrowDirection" },
			{ "UIKit.UINavigationControllerOperation", "UINavigationController.Operation" },
			{ "UIKit.UINavigationItemLargeTitleDisplayMode", "UINavigationItem.LargeTitleDisplayMode" },
			{ "UIKit.UINotificationFeedbackType", "UINotificationFeedbackGenerator.FeedbackType" },
			{ "UIKit.UIPageViewControllerNavigationDirection", "UIPageViewController.NavigationDirection" },
			{ "UIKit.UIPageViewControllerNavigationOrientation", "UIPageViewController.NavigationOrientation" },
			{ "UIKit.UIPageViewControllerSpineLocation", "UIPageViewController.SpineLocation" },
			{ "UIKit.UIPageViewControllerTransitionStyle", "UIPageViewController.TransitionStyle" },
			{ "UIKit.UIPreferredPresentationStyle", "NSItemProvider.PreferredPresentationStyle" },
			{ "UIKit.UIPressPhase", "UIPress.Phase" },
			{ "UIKit.UIPressType", "UIPress.PressType" },
			{ "UIKit.UIPreviewActionStyle", "UIPreviewAction.Style" },
			{ "UIKit.UIPrinterCutterBehavior", "UIPrinter.CutterBehavior" },
			{ "UIKit.UIPrinterJobTypes", "UIPrinter.JobTypes" },
			{ "UIKit.UIPrintInfoDuplex", "UIPrintInfo.Duplex" },
			{ "UIKit.UIPrintInfoOrientation", "UIPrintInfo.Orientation" },
			{ "UIKit.UIPrintInfoOutputType", "UIPrintInfo.OutputType" },
			{ "UIKit.UIProgressViewStyle", "UIProgressView.Style" },
			{ "UIKit.UIPushBehaviorMode", "UIPushBehavior.Mode" },
			{ "UIKit.UIScreenOverscanCompensation", "UIScreen.OverscanCompensation" },
			{ "UIKit.UIScrollViewContentInsetAdjustmentBehavior", "UIScrollView.ContentInsetAdjustmentBehavior" },
			{ "UIKit.UIScrollViewIndexDisplayMode", "UIScrollView.IndexDisplayMode" },
			{ "UIKit.UIScrollViewIndicatorStyle", "UIScrollView.IndicatorStyle" },
			{ "UIKit.UIScrollViewKeyboardDismissMode", "UIScrollView.KeyboardDismissMode" },
			{ "UIKit.UISearchBarIcon", "UISearchBar.Icon" },
			{ "UIKit.UISearchBarStyle", "UISearchBar.Style" },
			{ "UIKit.UISegmentedControlSegment", "UISegmentedControl.Segment" },
			{ "UIKit.UISplitViewControllerDisplayMode", "UISplitViewController.DisplayMode" },
			{ "UIKit.UISplitViewControllerPrimaryEdge", "UISplitViewController.PrimaryEdge" },
			{ "UIKit.UIStackViewAlignment", "UIStackView.Alignment" },
			{ "UIKit.UIStackViewDistribution", "UIStackView.Distribution" },
			{ "UIKit.UISwipeGestureRecognizerDirection", "UISwipeGestureRecognizer.Direction" },
			{ "UIKit.UISystemAnimation", "UIView.SystemAnimation" },
			{ "UIKit.UITabBarItemPositioning", "UITabBar.ItemPositioning" },
			{ "UIKit.UITabBarSystemItem", "UITabBarItem.SystemItem" },
			{ "UIKit.UITableViewCellDragState", "UITableViewCell.DragState" },
			{ "UIKit.UITableViewCellEditingStyle", "UITableViewCell.EditingStyle" },
			{ "UIKit.UITableViewCellFocusStyle", "UITableViewCell.FocusStyle" },
			{ "UIKit.UITableViewCellSelectionStyle", "UITableViewCell.SelectionStyle" },
			{ "UIKit.UITableViewCellSeparatorStyle", "UITableViewCell.SeparatorStyle" },
			{ "UIKit.UITableViewCellStyle", "UITableViewCell.CellStyle" },
			{ "UIKit.UITableViewDropIntent", "UITableViewDropProposal.Intent" },
			{ "UIKit.UITableViewRowActionStyle", "UITableViewRowAction.Style" },
			{ "UIKit.UITableViewRowAnimation", "UITableView.RowAnimation" },
			{ "UIKit.UITableViewScrollPosition", "UITableView.ScrollPosition" },
			{ "UIKit.UITableViewSeparatorInsetReference", "UITableView.SeparatorInsetReference" },
			{ "UIKit.UITableViewStyle", "UITableView.Style" },
			{ "UIKit.UITextBorderStyle", "UITextField.BorderStyle" },
			{ "UIKit.UITextDropAction", "UITextDropProposal.Action" },
			{ "UIKit.UITextDropPerformer", "UITextDropProposal.Performer" },
			{ "UIKit.UITextDropProgressMode", "UITextDropProposal.ProgressMode" },
			{ "UIKit.UITextFieldDidEndEditingReason", "UITextField.DidEndEditingReason" },
			{ "UIKit.UITextFieldViewMode", "UITextField.ViewMode" },
			{ "UIKit.UITouchPhase", "UITouch.Phase" },
			{ "UIKit.UITouchProperties", "UITouch.Properties" },
			{ "UIKit.UITouchType", "UITouch.TouchType" },
			{ "UIKit.UIUserInterfaceStyle", "UITraitCollection.UIUserInterfaceStyle" },
			{ "UIKit.UIViewAnimationCurve", "UIView.AnimationCurve" },
			{ "UIKit.UIViewAnimationOptions", "UIView.AnimationOptions" },
			{ "UIKit.UIViewAnimationTransition", "UIView.AnimationTransition" },
			{ "UIKit.UIViewAutoresizing", "UIView.AutoresizingMask" },
			{ "UIKit.UIViewContentMode", "UIView.ContentMode" },
			{ "UIKit.UIViewKeyframeAnimationOptions", "UIView.KeyframeAnimationOptions" },
			{ "UIKit.UIViewTintAdjustmentMode", "UIView.TintAdjustmentMode" },
			{ "UIKit.UIWebPaginationBreakingMode", "UIWebView.PaginationBreakingMode" },
			{ "UIKit.UIWebPaginationMode", "UIWebView.PaginationMode" },
			{ "UIKit.UIWebViewNavigationType", "UIWebView.NavigationType" },
			{ "UIKit.UIContextualActionStyle", "UIContextualAction.Style" },
			{ "UIKit.UIDocumentBrowserViewControllerImportMode", "UIDocumentBrowserViewController.ImportMode" },
			{ "UIKit.UIDocumentBrowserViewControllerBrowserUserInterfaceStyle", "UIDocumentBrowserViewController.BrowserUserInterfaceStyle" },
	    		{ "UIKit.UIDocumentBrowserImportMode", "UIDocumentBrowserViewController.ImportMode" },
			{ "UIKit.UIDocumentBrowserUserInterfaceStyle", "UIDocumentBrowserViewController.BrowserUserInterfaceStyle" },
			// UserNotifications
			{ "UserNotifications.UNErrorCode", "UNError.Code" },
			// WatchConnectivity
			{ "WatchConnectivity.WCErrorCode", "WCError.Code" },
			// WebKit
			{ "WebKit.WKErrorCode", "WKError.Code" },
		};
	}
}
