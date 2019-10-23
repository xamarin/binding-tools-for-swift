// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace SwiftReflector.Importing {
	public partial class TypeAggregator {
		static partial void ModulesToSkipMacOS (ref HashSet<string> result) { result = macOSModulesToSkip; }
		static HashSet<string> macOSModulesToSkip = new HashSet<string> () {
			"AdSupport",
	    		"BusinessChat",
			"CoreAnimation",
	    		"CoreMidi", // -> CoreMIDI
			"CoreWlan",
	    		"ImageKit",
			"iTunesLibrary",
	    		"MobileCoreServices",
			"NaturalLanguage",
	    		"Network",
			"ObjCRuntime",
	    		"OpenTK",
			"PdfKit", // -> PdfKit
	    		"PrintCore",
			"QTKit",
	    		"QuartzComposer",
			"QuickLookUI",
	    		"Registrar",
			"SearchKit",
	    		"System",
			"System.Drawing",
	    		"System.Net.Http",
			"UserNotifications",
	    		"VideoSubscriberAccount",
			"Xamarin.Utils",
		};

		static partial void AvailableMapMacOS(ref Dictionary<string, string> result)
		{
			result = MacOSAvailableMap;
		}

		static string macos10_10 = "macOS 10.10, *";
		static Dictionary<string, String> MacOSAvailableMap = new Dictionary<string, string> () {
			{ "Foundation.NSItemProviderErrorCode", macos10_10 },
			{ "Foundation.NSLengthFormatterUnit", macos10_10 },
			{ "Foundation.NSMassFormatterUnit", macos10_10 },
			{ "Foundation.NSQualityOfService", macos10_10 }
		};

		static partial void TypesToSkipMacOS (ref HashSet<string> result) { result = macOSTypesToSkip; }
		static HashSet<string> macOSTypesToSkip = new HashSet<string> () {
			"AppKit.NSFileWrapperReadingOptions", // same name as Foundation.NSFileWrapperReadingOptions
			"Foundation.NSWritingDirection", // obsolete
			"Foundation.NSAppleEventDescriptorType",
	    		"Foundation.NSAttributedStringEnumeration",
			"Foundation.NSBundleExecutableArchitecture",
	    		"Foundation.NSCocoaError",
			"Foundation.NSDateComponentsWrappingBehavior",
			"Foundation.NSDocumentType",
	    		"Foundation.NSEnumerationOptions",
			"Foundation.NSFileType",
			"Foundation.NSItemDownloadingStatus",
	    		"Foundation.NSLinguisticTagUnit",
	    		"Foundation.NSNetServicesStatus",
			"Foundation.NSNotificationFlags",
	    		"Foundation.NSNotificationSuspensionBehavior",
	    		"Foundation.NSRunLoopMode",
			"Foundation.NSStringDrawingOptions",
	    		"Foundation.NSStringEncoding",
			"Foundation.NSTextWritingDirection",
	    		"Foundation.NSUbiquitousKeyValueStoreChangeReason",
	    		"Foundation.NSUrlError",
	    		"Foundation.NSUrlErrorCancelledReason",
			"Foundation.NSUrlRelationship",
	    		"Foundation.NSUserDefaultsType",
			"Foundation.NSUserNotificationActivationType",
	    		"Foundation.NSAlignmentOptions",
	    		"Foundation.NSLigatureType",
			// swift 4.2+
			"Foundation.NSIso8601DateFormatOptions", // 10.12
			// swift 4.2+
	    		"Foundation.NSUrlSessionDelayedRequestDisposition", // 10.13
			// swift 4.2+
			"Foundation.NSUrlSessionTaskMetricsResourceFetchType", // 10.12
		};

		static partial void TypeNamesToMapMacOS (ref Dictionary<string, string> result) { result = macOSTypeNamesToMap; }
		static Dictionary<string, string> macOSTypeNamesToMap = new Dictionary<string, string> {
			{ "Foundation.NSActivityOptions", "ProcessInfo.ActivityOptions" },
			{ "Foundation.NSAppleEventSendOptions", "NSAppleEventDescriptor.SendOptions" },
			{ "Foundation.NSBundle", "Bundle" },
			{ "Foundation.NSByteCountFormatterCountStyle", "ByteCountFormatter.CountStyle" },
			{ "Foundation.NSByteCountFormatterUnits", "ByteCountFormatter.Units" },
			{ "Foundation.NSCalculationError", "NSDecimalNumber.CalculationError" },
			{ "Foundation.NSCalendarOptions", "NSCalendar.Options" },
			{ "Foundation.NSCalendarType", "NSCalendar.Identifier" },
			{ "Foundation.NSCalendarUnit", "NSCalendar.Unit" },
			{ "Foundation.NSComparisonPredicateModifier", "NSComparisonPredicate.Modifier" },
			{ "Foundation.NSComparisonPredicateOptions", "NSComparisonPredicate.Options" },
			{ "Foundation.NSCompoundPredicateType", "NSCompoundPredicate.LogicalType" },
			{ "Foundation.NSComparisonResult", "ComparisonResult" },
			{ "Foundation.NSDataBase64DecodingOptions", "NSData.Base64DecodingOptions" },
			{ "Foundation.NSDataBase64EncodingOptions", "NSData.Base64EncodingOptions" },
			{ "Foundation.NSDataWritingOptions", "NSData.WritingOptions" },
			{ "Foundation.NSDataSearchOptions", "NSData.SearchOptions" },
			{ "Foundation.NSDateComponentsFormatterUnitsStyle", "DateComponentsFormatter.UnitsStyle" },
			{ "Foundation.NSDateFormatterBehavior", "DateFormatter.Behavior" },
			{ "Foundation.NSDateFormatterStyle", "DateFormatter.Style" },
			{ "Foundation.NSDateIntervalFormatterStyle", "DateIntervalFormatter.Style" },
			{ "Foundation.NSDecimal", "Decimal" },
			{ "Foundation.NSDecodingFailurePolicy", "NSCoder.DecodingFailurePolicy" },
			{ "Foundation.NSDirectoryEnumerationOptions", "FileManager.DirectoryEnumerationOptions" },
			{ "Foundation.NSEnergyFormatterUnit", "EnergyFormatter.Unit" },
			{ "Foundation.NSExpressionType", "NSExpression.ExpressionType" },
			{ "Foundation.NSFileCoordinatorReadingOptions", "NSFileCoordinator.ReadingOptions" },
			{ "Foundation.NSFileCoordinatorWritingOptions", "NSFileCoordinator.WritingOptions" },
			{ "Foundation.NSFileManagerItemReplacementOptions", "FileManager.ItemReplacementOptions" },
			{ "Foundation.NSFileManagerUnmountOptions", "FileManager.UnmountOptions" },
			{ "Foundation.NSFileVersionAddingOptions", "NSFileVersion.AddingOptions" },
			{ "Foundation.NSFileVersionReplacingOptions", "NSFileVersion.ReplacingOptions" },
			{ "Foundation.NSFileWrapperReadingOptions", "FileWrapper.ReadingOptions" },
			{ "Foundation.NSFileWrapperWritingOptions", "FileWrapper.WritingOptions" },
			{ "Foundation.NSFormattingContext", "Formatter.Context" },
			{ "Foundation.NSHttpCookieAcceptPolicy", "HTTPCookie.AcceptPolicy" },
			{ "Foundation.NSIndexPath", "IndexPath" },
			{ "Foundation.NSItemProviderErrorCode", "NSItemProvider.ErrorCode" },
			{ "Foundation.NSJsonReadingOptions", "JSONSerialization.ReadingOptions" },
			{ "Foundation.NSJsonWritingOptions", "JSONSerialization.WritingOptions" },
			{ "Foundation.NSLengthFormatterUnit", "LengthFormatter.Unit" },
			{ "Foundation.NSLinguisticTaggerOptions", "NSLinguisticTagger.Options" },
			{ "Foundation.NSLocaleLanguageDirection", "NSLocale.LanguageDirection"},
			{ "Foundation.NSMachPortRights", "NSMachPort.Options" },
			{ "Foundation.NSMassFormatterUnit", "MassFormatter.Unit" },
			{ "Foundation.NSMatchingFlags", "NSRegularExpression.MatchingFlags" },
			{ "Foundation.NSMatchingOptions", "NSRegularExpression.MatchingOptions" },
			{ "Foundation.NSMeasurementFormatterUnitOptions", "MeasurementFormatter.UnitOptions" },
			{ "Foundation.NSNetServiceOptions", "NetService.Options" },
			{ "Foundation.NSNotificationCoalescing", "NotificationQueue.NotificationCoalescing" },
			{ "Foundation.NSNumberFormatterBehavior", "NumberFormatter.Behavior" },
			{ "Foundation.NSNumberFormatterPadPosition", "NumberFormatter.PadPosition" },
			{ "Foundation.NSNumberFormatterRoundingMode", "NumberFormatter.RoundingMode" },
			{ "Foundation.NSNumberFormatterStyle", "NumberFormatter.Style" },
			{ "Foundation.NSOperatingSystemVersion", "OperatingSystemVersion" },
			{ "Foundation.NSOperationQueuePriority", "Operation.QueuePriority" },
			{ "Foundation.NSPostingStyle", "NotificationQueue.PostingStyle" },
			{ "Foundation.NSPredicateOperatorType", "NSComparisonPredicate.Operator" },
			{ "Foundation.NSProcessInfoThermalState", "ProcessInfo.ThermalState" },
			{ "Foundation.NSPropertyListFormat", "PropertyListSerialization.PropertyListFormat" },
			{ "Foundation.NSPropertyListMutabilityOptions", "PropertyListSerialization.MutabilityOptions" },
			{ "Foundation.NSPropertyListWriteOptions", "PropertyListSerialization.WriteOptions" },
			{ "Foundation.NSPropertyListReadOptions", "PropertyListSerialization.ReadOptions"},
			{ "Foundation.NSRegularExpressionOptions", "NSRegularExpression.Options" },
			{ "Foundation.NSRoundingMode", "NSDecimalNumber.RoundingMode" },
			{ "Foundation.NSSearchPathDirectory", "FileManager.SearchPathDirectory" },
			{ "Foundation.NSSearchPathDomain", "FileManager.SearchPathDomainMask" },
			{ "Foundation.NSStreamEvent", "Stream.Event" },
			{ "Foundation.NSStreamSocketSecurityLevel", "StreamSocketSecurityLevel" },
			{ "Foundation.NSStreamServiceType", "StreamNetworkServiceTypeValue" },
			{ "Foundation.NSStreamStatus", "Stream.Status" },
			{ "Foundation.NSStringCompareOptions", "NSString.CompareOptions" },
			{ "Foundation.NSStringTransform", "StringTransform" },
			{ "Foundation.NSTaskTerminationReason", "Process.TerminationReason" },
			{ "Foundation.NSTextCheckingType", "NSTextCheckingResult.CheckingType" },
			{ "Foundation.NSTimeZoneNameStyle", "NSTimeZone.NameStyle" },
			{ "Foundation.NSUrlBookmarkCreationOptions", "NSURL.BookmarkCreationOptions" },
			{ "Foundation.NSUrlBookmarkResolutionOptions", "NSURL.BookmarkResolutionOptions" },
			{ "Foundation.NSUrlCacheStoragePolicy", "URLCache.StoragePolicy" },
			{ "Foundation.NSUrlCredentialPersistence", "URLCredential.Persistence" },
			{ "Foundation.NSUrlRequestCachePolicy", "NSURLRequest.CachePolicy" },
			{ "Foundation.NSUrlRequestNetworkServiceType", "NSURLRequest.NetworkServiceType" },
			{ "Foundation.NSUrlSessionAuthChallengeDisposition", "URLSession.AuthChallengeDisposition" },
			{ "Foundation.NSUrlSessionResponseDisposition", "URLSession.ResponseDisposition" },
			{ "Foundation.NSUrlSessionTaskState", "URLSessionTask.State" },
			{ "Foundation.NSVolumeEnumerationOptions", "FileManager.VolumeEnumerationOptions" },
			{ "Foundation.NSDateComponentsFormatterZeroFormattingBehavior", "DateComponentsFormatter.ZeroFormattingBehavior" },
			{ "Foundation.NSPersonNameComponentsFormatterOptions", "PersonNameComponentsFormatter.Options" },
			{ "Foundation.NSPersonNameComponentsFormatterStyle", "PersonNameComponentsFormatter.Style" },
			{ "Foundation.NSQualityOfService", "QualityOfService" },
			{ "Foundation.NSDataReadingOptions", "NSData.ReadingOptions" },
			{ "Foundation.NSFormattingUnitStyle", "Formatter.UnitStyle" },
		};
	}
}
