// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace SwiftReflector.IOUtils {
	public enum CFKey {
		CFAppleHelpAnchor,
		CFBundleAllowMixedLocalizations,
		CFBundleDevelopmentRegion,
		CFBundleDisplayName,
		CFBundleDocumentTypes,
		CFBundleExecutable,
		CFBundleHelpBookFolder,
		CFBundleHelpBookName,
		CFBundleIconFile,
		CFBundleIconFiles,
		CFBundleIcons,
		CFBundleIdentifier,
		CFBundleInfoDictionaryVersion,
		CFBundleLocalizations,
		CFBundleName,
		CFBundlePackageType,
		CFBundleShortVersionString,
		CFBundleSpokenName,
		CFBundleURLTypes,
		CFBundleVersion,
		CFPlugInDynamicRegistration,
		CFPlugInDynamicRegistrationFunction,
		CFPlugInFactories,
		CFPlugInTypes,
		CFPlugInUnloadFunction,
		CFBundleSignature
	}

	public enum LSKey {
		LSApplicationCategoryType,
		LSApplicationQueriesSchemes,
		LSArchitecturePriority,
		LSBackgroundOnly,
		LSEnvironment,
		LSFileQuarantineEnabled,
		LSFileQuarantineExcludedPathPatterns,
		LSGetAppDiedEvents,
		LSMinimumSystemVersion,
		LSMinimumSystemVersionByArchitecture,
		LSMultipleInstancesProhibited,
		LSRequiresIPhoneOS,
		LSRequiresNativeExecution,
		LSSupportsOpeningDocumentsInPlace,
		LSUIElement,
		LSUIPresentationMode,
		LSVisibleInClassic,
		MinimumOSVersion
	}

	public enum IOSKey {
		CoreSpotlightContinuation,
		INAlternativeAppNames,
		MKDirectionsApplicationSupportedModes,
		UIAppFonts,
		UIApplicationExitsOnSuspend,
		UIApplicationShortcutItems,
		UIApplicationShortcutWidget,
		UIBackgroundModes,
		UIDeviceFamily,
		UIFileSharingEnabled,
		UIInterfaceOrientation,
		UILaunchImageFile,
		UILaunchImages,
		UILaunchStoryboardName,
		UILaunchStoryboards,
		UINewsstandApp,
		UIPrerenderedIcon,
		UIRequiredDeviceCapabilities,
		UIRequiresPersistentWiFi,
		UIStatusBarHidden,
		UIStatusBarStyle,
		UISupportedExternalAccessoryProtocols,
		UISupportedInterfaceOrientations,
		UISupportsDocumentBrowser,
		UIUserInterfaceStyle,
		UIViewControllerBasedStatusBarAppearance,
		UIViewEdgeAntialiasing,
		UIViewGroupOpacity,
		UIWhitePointAdaptivityStyle
	}

	public enum WatchOSKey {
		CLKComplicationSupportedFamilies,
		CLKComplicationPrincipalClass,
		WKAppBundleIdentifier,
		WKBackgroundModes,
		WKCompanionAppBundleIdentifier,
		WKExtensionDelegateClassName,
		WKWatchKitApp
	}
}
