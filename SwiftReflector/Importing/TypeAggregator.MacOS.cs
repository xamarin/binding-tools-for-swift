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
			"Microsoft.CodeAnalysis",
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
			"System.Runtime.CompilerServices",
			"UserNotifications",
	    		"VideoSubscriberAccount",
			"Xamarin.Bundler",
			"Xamarin.Utils",
		};

		static partial void AvailableMapMacOS(ref Dictionary<string, string> result)
		{
			result = MacOSAvailableMap;
		}

		static string macos10_10 = "macOS 10.10, *";
		static string macos10_11 = "macOS 10.11, *";
		static string macos10_11_4 = "macOS 10.11.4, *";
		static string macos10_12 = "macOS 10.12, *";
		static string macos10_12_2 = "macOS 10.12.2, *";
		static string macos10_13 = "macOS 10.13, *";
		static string macos10_14 = "macOS 10.14, *";
		static string macos10_15 = "macOS 10.15, *";
		static Dictionary<string, string> MacOSAvailableMap = new Dictionary<string, string> () {
			// AppKit
			{ "AppKit.NSDirectionalEdgeInsets", macos10_15 },
			{ "AppKit.NSTabViewControllerTabStyle", macos10_10 },
			{ "AppKit.NSViewControllerTransitionOptions", macos10_10 },
			{ "AppKit.NSVisualEffectBlendingMode", macos10_10 },
			{ "AppKit.NSVisualEffectMaterial", macos10_10 },
			{ "AppKit.NSVisualEffectState", macos10_10 },
			{ "AppKit.NSWindowTitleVisibility", macos10_10 },
			// AVFoundation
			{ "AVFoundation.AVAudio3DMixingRenderingAlgorithm", macos10_10 },
			{ "AVFoundation.AVAudioCommonFormat", macos10_10 },
			{ "AVFoundation.AVAudioEnvironmentDistanceAttenuationModel", macos10_10 },
			{ "AVFoundation.AVAudioUnitDistortionPreset", macos10_10 },
			{ "AVFoundation.AVAudioUnitEQFilterType", macos10_10 },
			{ "AVFoundation.AVAudioUnitReverbPreset", macos10_10 },
			{ "AVFoundation.AVAuthorizationStatus", macos10_14 },
			{ "AVFoundation.AVMetadataObjectType", macos10_10 },
			{ "AVFoundation.AVMusicTrackLoopCount", macos10_10 },
			{ "AVFoundation.AVQueuedSampleBufferRenderingStatus", macos10_10 },
			// CoreData
			{ "CoreData.NSBatchUpdateRequestResultType", macos10_10 },
			{ "CoreData.NSFetchedResultsChangeType", macos10_12 },
			// CoreLocation
			{ "CoreLocation.CLRegionState", macos10_10 },
			{ "CoreLocation.CLProximity", macos10_15 },
			// Foundation
			{ "Foundation.NSItemProviderErrorCode", macos10_10 },
			{ "Foundation.NSLengthFormatterUnit", macos10_10 },
			{ "Foundation.NSMassFormatterUnit", macos10_10 },
			{ "Foundation.NSQualityOfService", macos10_10 },
			// GameController
			{ "GameController.GCExtendedGamepadSnapshotData", macos10_11 },
			{ "GameController.GCExtendedGamepadSnapshotDataVersion", macos10_11 },
			{ "GameController.GCMicroGamepadSnapshotData", macos10_11 },
			{ "GameController.GCMicroGamepadSnapshotDataVersion", macos10_11 },
			{ "GameController.GCExtendedGamepadSnapShotDataV100", macos10_11 },
			// GameKit
			{ "GameKit.GKTurnBasedExchangeStatus", macos10_10 },
			// MapKit
			{ "MapKit.MKSearchCompletionFilterType", macos10_11_4 },
			// MediaPlayer
			{ "MediaPlayer.MPMediaType", macos10_12_2 },
			// Metal
			{ "Metal.MTLArgumentAccess", macos10_11 },
			{ "Metal.MTLArgumentType", macos10_11 },
			{ "Metal.MTLBlendFactor", macos10_11 },
			{ "Metal.MTLBlendOperation", macos10_11 },
			{ "Metal.MTLColorWriteMask", macos10_11 },
			{ "Metal.MTLCommandBufferError", macos10_11 },
			{ "Metal.MTLCommandBufferStatus", macos10_11 },
			{ "Metal.MTLCompareFunction", macos10_11 },
			{ "Metal.MTLCpuCacheMode", macos10_11 },
			{ "Metal.MTLCullMode", macos10_11 },
			{ "Metal.MTLDataType", macos10_11 },
			{ "Metal.MTLFeatureSet", macos10_11 },
			{ "Metal.MTLFunctionType", macos10_11 },
			{ "Metal.MTLIndexType", macos10_11 },
			{ "Metal.MTLLibraryError", macos10_11 },
			{ "Metal.MTLLoadAction", macos10_11 },
			{ "Metal.MTLMultisampleDepthResolveFilter", macos10_14 },
			{ "Metal.MTLPipelineOption", macos10_11 },
			{ "Metal.MTLPrimitiveType", macos10_11 },
			{ "Metal.MTLPurgeableState", macos10_11 },
			{ "Metal.MTLRenderStages", macos10_13 },
			{ "Metal.MTLSamplerAddressMode", macos10_11 },
			{ "Metal.MTLSamplerMinMagFilter", macos10_11 },
			{ "Metal.MTLSamplerMipFilter", macos10_11 },
			{ "Metal.MTLStencilOperation", macos10_11 },
			{ "Metal.MTLStoreAction", macos10_11 },
			{ "Metal.MTLTextureType", macos10_11 },
			{ "Metal.MTLTriangleFillMode", macos10_11 },
			{ "Metal.MTLVertexFormat", macos10_11 },
			{ "Metal.MTLVertexStepFunction", macos10_11 },
			{ "Metal.MTLVisibilityResultMode", macos10_11 },
			{ "Metal.MTLWinding", macos10_11 },
			// MultipeerConnectivity
			{ "MultipeerConnectivity.MCEncryptionPreference", macos10_11 },
			{ "MultipeerConnectivity.MCError", macos10_10 },
			{ "MultipeerConnectivity.MCSessionSendDataMode", macos10_10 },
			{ "MultipeerConnectivity.MCSessionState", macos10_10 },
			// NetworkExtension
			{ "NetworkExtension.NEAppProxyFlowError", macos10_11 },
			{ "NetworkExtension.NEEvaluateConnectionRuleAction", macos10_11 },
			{ "NetworkExtension.NEFilterManagerError", macos10_11 },
			{ "NetworkExtension.NEOnDemandRuleAction", macos10_11 },
			{ "NetworkExtension.NEOnDemandRuleInterfaceType", macos10_12 },
			{ "NetworkExtension.NEProviderStopReason", macos10_11 },
			{ "NetworkExtension.NETunnelProviderError", macos10_11 },
			{ "NetworkExtension.NEVpnError", macos10_11 },
			{ "NetworkExtension.NEVpnIke2DeadPeerDetectionRate", macos10_11 },
			{ "NetworkExtension.NEVpnIke2DiffieHellman", macos10_11 },
			{ "NetworkExtension.NEVpnIke2EncryptionAlgorithm", macos10_11 },
			{ "NetworkExtension.NEVpnIke2IntegrityAlgorithm", macos10_11 },
			{ "NetworkExtension.NEVpnIkeAuthenticationMethod", macos10_11 },
			{ "NetworkExtension.NEVpnStatus", macos10_11 },
			{ "NetworkExtension.NWPathStatus", macos10_11 },
			{ "NetworkExtension.NWTcpConnectionState", macos10_11 },
			{ "NetworkExtension.NWUdpSessionState", macos10_11 },
			// SceneKit
			{ "SceneKit.SCNAntialiasingMode", macos10_10 },
			{ "SceneKit.SCNPhysicsCollisionCategory", macos10_10 },
			{ "SceneKit.SCNPhysicsSearchMode", macos10_10 },
			// Security
			{ "Security.SecAccessControlCreateFlags", macos10_10 },
			// SpriteKit
			{ "SpriteKit.SKParticleRenderOrder", macos10_11 },
			{ "SpriteKit.SKUniformType", macos10_10 },
		};

		static partial void TypesToSkipMacOS (ref HashSet<string> result)
		{
			macOSTypesToSkip.UnionWith (iOSTypesToSkip);
			result = macOSTypesToSkip;
		}
		static HashSet<string> macOSTypesToSkip = new HashSet<string> () {
			// AppKit
			"AppKit.HfsTypeCode",
			"AppKit.NSFileWrapperReadingOptions", // same name as Foundation.NSFileWrapperReadingOptions
			"AppKit.NSAlertButtonReturn", // not an enum
			"AppKit.NSAlertType", // can't find it
			"AppKit.NSApplicationLayoutDirection", // can't find it
			"AppKit.NSColorPanelFlags", // can't find it
			"AppKit.NSCollectionLayoutAnchorOffsetType",
			"AppKit.NSComposite", // not an enum
			"AppKit.NSEventModifierMask",
			"AppKit.NSEventMouseSubtype", // not in AppKit
			"AppKit.NSFontCollectionAction", // not an enum
			"AppKit.NSFontError", // not an enum
			"AppKit.NSFontPanelMode", // replaced
			"AppKit.NSFunctionKey", // not an enum
			"AppKit.NSGLColorBuffer", // can't find it
			"AppKit.NSGLFormat", // can't find it
			"AppKit.NSGLTextureCubeMap", // can't find it
			"AppKit.NSGLTextureTarget", // can't find it
			"AppKit.NSGlyphInscription", // marked unavailable
			"AppKit.NSGlyphStorageOptions", // can't find it
			"AppKit.NSImageScale", // not an enum
			"AppKit.NSKey", // can't find it
			"AppKit.NSLineMovementDirection", // marked unavailable
			"AppKit.NSLineSweepDirection", // marked unavailable
			"AppKit.NSOpenGLProfile", // not an enum
			"AppKit.NSPanelButtonType", // can't find it
			"AppKit.NSPopoverCloseReason", // not an enum
			"AppKit.NSProgressIndicatorThickness", // marked unavailable
			"AppKit.NSRulerViewUnits", // not an enum
			"AppKit.NSRunResponse", // not an enum
			"AppKit.NSStatusItemLength", // not an enum
			"AppKit.NSSurfaceOrder", // not an enum
			"AppKit.NSSystemDefinedEvents", // can't find it
			"AppKit.NSTextListMarkerFormats", // can't find it
			"AppKit.NSTextStorageEditedFlags", // can't find it
			"AppKit.NSType", // can't find it
			"AppKit.NSUnderlinePattern", // not an enum
			// AudioToolbox
			"AudioToolbox.AudioQueueChannelAssignment", // macOS 10.15+
			"AudioToolbox.AudioSessionInterruptionType", // iOS only
			// AudioUnit
			"AudioUnit.AudioUnitRemoteControlEvent", // iOS and tvOS only
			// AuthenticationServices
			"AuthenticationServices.ASAuthorizationAppleIdButtonStyle",
			"AuthenticationServices.ASAuthorizationAppleIdButtonType",
			"AuthenticationServices.ASAuthorizationAppleIdProviderCredentialState",
			"AuthenticationServices.ASAuthorizationOperation",
			// AVFoundation
			"AVAudioSession", // marked unavailable
			"AVFoundation.AVAudioSessionCategoryOptions", // macOS 10.15+
			"AVFoundation.AVAudioSessionErrorCode", // macOS 10.15+
			"AVFoundation.AVAudioSessionInterruptionOptions", // macOS 10.15+
			"AVFoundation.AVAudioSessionInterruptionType", // macOS 10.15+
			"AVFoundation.AVAudioSessionIOType",
			"AVFoundation.AVAudioSessionPortOverride", // macOS 10.15+
			"AVFoundation.AVAudioSessionPromptStyle",
			"AVFoundation.AVAudioSessionRecordPermission", // macOS 10.15+
			"AVFoundation.AVAudioSessionRouteChangeReason", // macOS 10.15+
			"AVFoundation.AVAudioSessionRouteSharingPolicy", // macOS 10.15+
			"AVFoundation.AVAudioSessionSetActiveOptions", // macOS 10.15+
			"AVFoundation.AVAudioSessionSilenceSecondaryAudioHintType", // macOS 10.15+
			"AVFoundation.AVCaptureAutoFocusRangeRestriction", // marked unavailable
			"AVFoundation.AVCaptureAutoFocusSystem", // marked unavailable
			"AVFoundation.AVCaptureLensStabilizationStatus", // marked unavailable
			"AVFoundation.AVCaptureOutputDataDroppedReason", // marked unavailable
			"AVFoundation.AVCaptureVideoStabilizationMode", // marked unavailable
			"AVFoundation.AVCaptureWhiteBalanceChromaticityValues", // marked unavailable
			"AVFoundation.AVCaptureWhiteBalanceGains", // marked unavailable
			"AVFoundation.AVCaptureWhiteBalanceTemperatureAndTintValues", // marked unavailable
			"AVFoundation.AVContentKeyResponseDataType",
			"AVFoundation.AVDepthDataAccuracy", // macOS 10.13+
			"AVFoundation.AVDepthDataQuality", // macOS 10.13+
			"AVFoundation.AVMetadataObjectType", // marked
			"AVFoundation.AVSpeechBoundary", // macOS 10.14+
			// AVKit
			"AVKit.AVKitError", // not on macOS
			// CoreFoundation
			"CoreFoundation.DispatchBlockFlags",
			"CoreFoundation.DispatchQualityOfService",
			"CoreFoundation.CFStringTransform",
			"CoreFoundation.OSLogLevel",
			// CoreGraphics
			"CoreGraphics.CGPdfTagType",
			// CoreMedia
			"CoreMedia.CMSampleBufferAttachmentKey",
			// CoreServices
			"CoreServices.FSEvent", // not a struct
			"CoreServices.LSResult", // not an enum
			// CoreVideo
			"CoreVideo.CVImageBufferAlphaChannelMode",
			// Foundation
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
			"Foundation.NSIso8601DateFormatOptions", // 10.12; swift 4.2+
			"Foundation.NSUrlErrorNetworkUnavailableReason",
	    		"Foundation.NSUrlSessionDelayedRequestDisposition", // 10.13; swift 4.2+
			"Foundation.NSUrlSessionWebSocketCloseCode",
			"Foundation.NSUrlSessionTaskMetricsResourceFetchType", // 10.12; swift 4.2+
			"Foundation.NSUrlSessionWebSocketMessageType",
			"Foundation.NSXpcConnectionOptions",
			// GLKit
			"GLKit.GLKViewDrawableColorFormat", // not on macOS
			"GLKit.GLKViewDrawableDepthFormat", // not on macOS
			"GLKit.GLKViewDrawableMultisample", // not on macOS
			"GLKit.GLKViewDrawableStencilFormat", // not on macOS
			// ImageCaptureCore
			"ImageCaptureCore.ICBrowsedDeviceType",
			"ImageCaptureCore.ICExifOrientationType",
			"ImageCaptureCore.ICReturnCode",
			"ImageCaptureCore.ICTransportType",
			// Intents
			"Intents.INCallCapability",
			"Intents.INCallCapabilityOptions",
			"Intents.INConditionalOperator",
			"Intents.INCallDestinationType",
			"Intents.INCallRecordTypeOptions",
			"Intents.INCallRecordType",
			"Intents.INIntentErrorCode",
			"Intents.INIntentHandlingStatus",
			"Intents.INInteractionDirection",
			"Intents.INMessageAttribute", // marked unavailable
			"Intents.INMessageAttributeOptions",
			"Intents.INPersonHandleType",
			"Intents.INPersonSuggestionType",
			"Intents.INSearchCallHistoryIntentResponseCode",
			"Intents.INSearchForMessagesIntentResponseCode",
			"Intents.INSendMessageRecipientUnsupportedReason",
			"Intents.INSendMessageIntentResponseCode",
			"Intents.INSetMessageAttributeIntentResponseCode", // marked unavailable
			"Intents.INStartAudioCallIntentResponseCode",
			"Intents.INStartVideoCallIntentResponseCode",
			"Intents.INMessageType",
			// LinkPresentation
			"LinkPresentation.LPErrorCode",
			// MapKit
			"MapKit.MKFeatureVisibility", // marked unavailable
			"MapKit.MKUserTrackingMode", // marked unavailable
			// Metal
			"Metal.MTLGpuFamily",
			// MetalPerformanceShaders
			"MetalPerformanceShaders.MPSRnnMatrixId",
			"MetalPerformanceShaders.MPSCnnBatchNormalizationFlags",
			"MetalPerformanceShaders.MPSCnnConvolutionGradientOption",
			"MetalPerformanceShaders.MPSCnnLossType",
			"MetalPerformanceShaders.MPSCnnReductionType",
			"MetalPerformanceShaders.MPSCnnWeightsQuantizationType",
			// Network
			"NetworkExtension.NENetworkRuleProtocol", // !! Can't figure out how to correctly map this into swift
			// the compiler is complaining about the name Protcol, but if you backtick quote it, you get other errors.
			// OpenGL
			"OpenGL.CGLErrorCode", // can't find it
			// Photos
			"Photos.FigExifCustomRenderedValue", // can't find it
			"Photos.PHPhotosError",
			// SafariServices
			"SafariServices.SFErrorCode", // not on macOS
			// Security
			"Security.AuthorizationStatus", // can't find it
			"Security.TlsCipherSuite",
			"Security.TlsCipherSuiteGroup",
			"Security.TlsProtocolVersion",
			// Social
			"Social.SLComposeViewControllerResult", // not on macOS
			// SoundAnalysis
			"SoundAnalysis.SNErrorCode",
			// StoreKit
			"StoreKit.SKCloudServiceAuthorizationStatus", // not on macOS
			"StoreKit.SKCloudServiceCapability", // not on macOS
			// VideoToolbox
			"VideoToolbox.VTAlphaChannelMode",
			// Vision
			"Vision.VNClassifyImageRequestRevision",
			"Vision.VNDetectFaceCaptureQualityRequestRevision",
			"Vision.VNDetectHumanRectanglesRequestRevision",
			"Vision.VNGenerateAttentionBasedSaliencyImageRequestRevision",
			"Vision.VNGenerateImageFeaturePrintRequestRevision",
			"Vision.VNGenerateObjectnessBasedSaliencyImageRequestRevision",
			"Vision.VNRecognizeAnimalsRequestRevision",
			"Vision.VNRecognizeTextRequestRevision",
			// WebKit
			"WebKit.DomCssRuleType", // no longer exists
			"WebKit.DomCssValueType", // no longer exists
			"WebKit.DomDelta", // no longer exists
			"WebKit.DomDocumentPosition", // no longer exists
			"WebKit.DomEventPhase", // no longer exists
			"WebKit.DomKeyLocation", // no longer exists
			"WebKit.DomNodeType", // no longer exists
			"WebKit.DomRangeCompareHow", // no longer exists
			"WebKit.WebActionMouseButton", // no longer exists
			"WebKit.WKSelectionGranularity", // not on macOS
		};

		static partial void TypeNamesToMapMacOS (ref Dictionary<string, string> result)
		{
			var combined = new Dictionary<string, string> ();
			foreach (var x in iOSTypeNamesToMap)
				combined [x.Key] = x.Value;
			// overwrites any iOS mappings with their macOS mappings if applicable
			foreach (var x in macOSTypeNamesToMap)
				combined [x.Key] = x.Value;
			result = combined;
		}
		static Dictionary<string, string> macOSTypeNamesToMap = new Dictionary<string, string> {
			// AppKit
			{ "AppKit.NSAccessibilityCustomRotorSearchDirection", "NSAccessibilityCustomRotor.SearchDirection" },
			{ "AppKit.NSAccessibilityCustomRotorType", "NSAccessibilityCustomRotor.RotorType" },
			{ "AppKit.NSAlertStyle", "NSAlert.Style" },
			{ "AppKit.NSAnimationBlockingMode", "NSAnimation.BlockingMode" },
			{ "AppKit.NSAnimationCurve", "NSAnimation.Curve" },
			{ "AppKit.NSApplicationActivationOptions", "NSApplication.ActivationOptions" },
			{ "AppKit.NSApplicationActivationPolicy", "NSApplication.ActivationPolicy" },
			{ "AppKit.NSApplicationDelegateReply", "NSApplication.DelegateReply" },
			{ "AppKit.NSApplicationOcclusionState", "NSApplication.OcclusionState" },
			{ "AppKit.NSApplicationPresentationOptions", "NSApplication.PresentationOptions" },
			{ "AppKit.NSApplicationPrintReply", "NSApplication.PrintReply" },
			{ "AppKit.NSApplicationTerminateReply", "NSApplication.TerminateReply" },
			{ "Foundation.NSBackgroundActivityResult", "NSBackgroundActivityScheduler.Result" },
			{ "AppKit.NSBackgroundStyle", "NSView.BackgroundStyle" },
			{ "AppKit.NSBackingStore", "NSWindow.BackingStoreType" },
			{ "AppKit.NSBezelStyle", "NSButton.BezelStyle" },
			{ "AppKit.NSBezierPathElement", "NSBezierPath.ElementType" },
			{ "AppKit.NSBitmapFormat", "NSBitmapImageRep.Format" },
			{ "AppKit.NSBitmapImageFileType", "NSBitmapImageRep.FileType" },
			{ "AppKit.NSBoxType", "NSBox.BoxType" },
			{ "AppKit.NSBrowserColumnResizingType", "NSBrowser.ColumnResizingType" },
			{ "AppKit.NSBrowserDropOperation", "NSBrowser.DropOperation" },
			{ "AppKit.NSButtonType", "NSButton.ButtonType" },
			{ "AppKit.NSCellAttribute", "NSCell.Attribute" },
			{ "AppKit.NSCellHit", "NSCell.HitResult" },
			{ "AppKit.NSCellImagePosition", "NSControl.ImagePosition" },
			{ "AppKit.NSCellStateValue", "NSCell.StateValue" },
			{ "AppKit.NSCellStyleMask", "NSCell.StyleMask" },
			{ "AppKit.NSCellType", "NSCell.CellType" },
			{ "AppKit.NSCloudKitSharingServiceOptions", "NSSharingService.CloudKitOptions" },
			{ "AppKit.NSCollectionUpdateAction", "NSCollectionView.UpdateAction" },
			{ "AppKit.NSCollectionViewDropOperation", "NSCollectionView.DropOperation" },
			{ "AppKit.NSCollectionViewItemHighlightState", "NSCollectionViewItem.HighlightState" },
			{ "AppKit.NSCollectionViewScrollDirection", "NSCollectionView.ScrollDirection" },
			{ "AppKit.NSCollectionViewScrollPosition", "NSCollectionView.ScrollPosition" },
			{ "AppKit.NSColorPanelMode", "NSColorPanel.Mode" },
			{ "AppKit.NSColorSpaceModel", "NSColorSpace.Model" },
			{ "AppKit.NSColorSystemEffect", "NSColor.SystemEffect" },
			{ "AppKit.NSColorType", "NSColor.ColorType" },
			{ "AppKit.NSControlCharacterAction", "NSLayoutManager.ControlCharacterAction" },
			{ "AppKit.NSControlSize", "NSControl.ControlSize" },
			{ "AppKit.NSCorrectionIndicatorType", "NSSpellChecker.CorrectionIndicatorType" },
			{ "AppKit.NSCorrectionResponse", "NSSpellChecker.CorrectionResponse" },
			{ "AppKit.NSDatePickerElementFlags", "NSDatePicker.ElementFlags" },
			{ "AppKit.NSDatePickerMode", "NSDatePicker.Mode" },
			{ "AppKit.NSDatePickerStyle", "NSDatePicker.Style" },
			{ "AppKit.NSDocumentChangeType", "NSDocument.ChangeType" },
			{ "AppKit.NSDrawerState", "NSDrawer.State" },
			{ "AppKit.NSEventButtonMask", "NSEvent.ButtonMask" },
			{ "AppKit.NSEventGestureAxis", "NSEvent.GestureAxis" },
			{ "AppKit.NSEventMask", "NSEvent.EventTypeMask" },
			{ "AppKit.NSEventModifierFlags", "NSEvent.ModifierFlags" },
			{ "AppKit.NSEventPhase", "NSEvent.Phase" },
			{ "AppKit.NSEventSubtype", "NSEvent.EventSubtype" },
			{ "AppKit.NSEventSwipeTrackingOptions", "NSEvent.SwipeTrackingOptions" },
			{ "AppKit.NSEventType", "NSEvent.EventType" },
			{ "AppKit.NSFontAssetRequestOptions", "NSFontAssetRequest.Options" },
			{ "AppKit.NSFontCollectionVisibility", "NSFontCollection.Visibility" },
			{ "AppKit.NSFontDescriptorSystemDesign", "NSFontDescriptor.SystemDesign" },
			{ "AppKit.NSFontPanelModeMask", "NSFontPanel.ModeMask" },
			{ "AppKit.NSGestureRecognizerState", "NSGestureRecognizer.State" },
			{ "AppKit.NSGlyphProperty", "NSLayoutManager.GlyphProperty" },
			{ "AppKit.NSGradientDrawingOptions", "NSGradient.DrawingOptions" },
			{ "AppKit.NSGradientType", "NSButton.GradientType" },
			{ "AppKit.NSGridCellPlacement", "NSGridCell.Placement" },
			{ "AppKit.NSGridRowAlignment", "NSGridRow.Alignment" },
			{ "AppKit.NSHapticFeedbackPattern", "NSHapticFeedbackManager.FeedbackPattern" },
			{ "AppKit.NSHapticFeedbackPerformanceTime", "NSHapticFeedbackManager.PerformanceTime" },
			{ "AppKit.NSImageCacheMode", "NSImage.CacheMode" },
			{ "AppKit.NSImageFrameStyle", "NSImageView.FrameStyle" },
			{ "AppKit.NSImageLayoutDirection", "NSImage.LayoutDirection" },
			{ "AppKit.NSImageLoadStatus", "NSImage.LoadStatus" },
			{ "AppKit.NSImageName", "NSImage.Name" },
			{ "AppKit.NSImageRepLoadStatus", "NSBitmapImageRep.LoadStatus" },
			{ "AppKit.NSImageResizingMode", "NSImage.ResizingMode" },
			{ "AppKit.NSLayoutAttribute", "NSLayoutConstraint.Attribute" },
			{ "AppKit.NSLayoutConstraintOrientation", "NSLayoutConstraint.Orientation" },
			{ "AppKit.NSLayoutFormatOptions", "NSLayoutConstraint.FormatOptions" },
			{ "AppKit.NSLayoutPriority", "NSLayoutConstraint.Priority" },
			{ "AppKit.NSLayoutRelation", "NSLayoutConstraint.Relation" },
			{ "AppKit.NSLevelIndicatorPlaceholderVisibility", "NSLevelIndicator.PlaceholderVisibility" },
			{ "AppKit.NSLevelIndicatorStyle", "NSLevelIndicator.Style" },
			{ "AppKit.NSLineCapStyle", "NSBezierPath.LineCapStyle" },
			{ "AppKit.NSLineJoinStyle", "NSBezierPath.LineJoinStyle" },
			{ "AppKit.NSMatrixMode", "NSMatrix.Mode" },
			{ "AppKit.NSMenuProperty", "NSMenu.Properties" },
			{ "AppKit.NSModalResponse", "NSApplication.ModalResponse" },
			{ "AppKit.NSOpenGLContextParameter", "NSOpenGLContext.Parameter" },
			{ "AppKit.NSPageControllerTransitionStyle", "NSPageController.TransitionStyle" },
			{ "AppKit.NSPasteboardContentsOptions", "NSPasteboard.ContentsOptions" },
			{ "AppKit.NSPasteboardReadingOptions", "NSPasteboard.ReadingOptions" },
			{ "AppKit.NSPasteboardWritingOptions", "NSPasteboard.WritingOptions" },
			{ "AppKit.NSPathStyle", "NSPathControl.Style" },
			{ "AppKit.NSPickerTouchBarItemControlRepresentation", "NSPickerTouchBarItem.ControlRepresentation" },
			{ "AppKit.NSPickerTouchBarItemSelectionMode", "NSPickerTouchBarItem.SelectionMode" },
			{ "AppKit.NSPointingDeviceType", "NSEvent.PointingDeviceType" },
			{ "AppKit.NSPopoverAppearance", "NSPopover.Appearance" },
			{ "AppKit.NSPopoverBehavior", "NSPopover.Behavior" },
			{ "AppKit.NSPopUpArrowPosition", "NSPopUpButton.ArrowPosition" },
			{ "AppKit.NSPressureBehavior", "NSEvent.PressureBehavior" },
			{ "AppKit.NSPrinterTableStatus", "NSPrinter.TableStatus" },
			{ "AppKit.NSPrintingOrientation", "NSPrintInfo.Orientation" },
			{ "AppKit.NSPrintingPageOrder", "NSPrintOperation.PageOrder" },
			{ "AppKit.NSPrintingPaginationMode", "NSPrintInfo.PaginationMode" },
			{ "AppKit.NSPrintPanelOptions", "NSPrintPanel.Options" },
			{ "AppKit.NSPrintRenderingQuality", "NSPrintOperation.RenderingQuality" },
			{ "AppKit.NSProgressIndicatorStyle", "NSProgressIndicator.Style" },
			{ "AppKit.NSRemoteNotificationType", "NSApplication.RemoteNotificationType" },
			{ "AppKit.NSRequestUserAttentionType", "NSApplication.RequestUserAttentionType" },
			{ "AppKit.NSRuleEditorNestingMode", "NSRuleEditor.NestingMode" },
			{ "AppKit.NSRuleEditorRowType", "NSRuleEditor.RowType" },
			{ "AppKit.NSRulerOrientation", "NSRulerView.Orientation" },
			{ "AppKit.NSSaveOperationType", "NSDocument.SaveOperationType" },
			{ "AppKit.NSScrollArrowPosition", "NSScroller.ArrowPosition" },
			{ "AppKit.NSScrollElasticity", "NSScrollView.Elasticity" },
			{ "AppKit.NSScrollerArrow", "NSScroller.Arrow" },
			{ "AppKit.NSScrollerKnobStyle", "NSScroller.KnobStyle" },
			{ "AppKit.NSScrollerPart", "NSScroller.Part" },
			{ "AppKit.NSScrollerStyle", "NSScroller.Style" },
			{ "AppKit.NSScrollViewFindBarPosition", "NSScrollView.FindBarPosition" },
			{ "AppKit.NSScrubberAlignment", "NSScrubber.Alignment" },
			{ "AppKit.NSScrubberMode", "NSScrubber.Mode" },
			{ "AppKit.NSSegmentDistribution", "NSSegmentedControl.Distribution" },
			{ "AppKit.NSSegmentStyle", "NSSegmentedControl.Style" },
			{ "AppKit.NSSegmentSwitchTracking", "NSSegmentedControl.SwitchTracking" },
			{ "AppKit.NSSelectionDirection", "NSWindow.SelectionDirection" },
			{ "AppKit.NSSharingContentScope", "NSSharingService.SharingContentScope" },
			{ "AppKit.NSSharingServiceName", "NSSharingService.Name" },
			{ "AppKit.NSSliderType", "NSSlider.SliderType" },
			{ "AppKit.NSSpeechBoundary", "NSSpeechSynthesizer.Boundary" },
			{ "AppKit.NSSpellingState", "NSAttributedString.SpellingState" },
			{ "AppKit.NSSplitViewDividerStyle", "NSSplitView.DividerStyle" },
			{ "AppKit.NSSplitViewItemBehavior", "NSSplitViewItem.Behavior" },
			{ "AppKit.NSStackViewDistribution", "NSStackView.Distribution" },
			{ "AppKit.NSStackViewGravity", "NSStackView.Gravity" },
			{ "AppKit.NSStackViewVisibilityPriority", "NSStackView.VisibilityPriority" },
			{ "AppKit.NSStatusItemBehavior", "NSStatusItem.Behavior" },
			{ "AppKit.NSTableColumnResizing", "NSTableColumn.ResizingOptions" },
			{ "AppKit.NSTableRowActionEdge", "NSTableView.RowActionEdge" },
			{ "AppKit.NSTableViewAnimation", "NSTableView.AnimationOptions" },
			{ "AppKit.NSTableViewColumnAutoresizingStyle", "NSTableView.ColumnAutoresizingStyle" },
			{ "AppKit.NSTableViewDraggingDestinationFeedbackStyle", "NSTableView.DraggingDestinationFeedbackStyle" },
			{ "AppKit.NSTableViewDropOperation", "NSTableView.DropOperation" },
			{ "AppKit.NSTableViewGridStyle", "NSTableView.GridLineStyle" },
			{ "AppKit.NSTableViewRowActionStyle", "NSTableViewRowAction.Style" },
			{ "AppKit.NSTableViewRowSizeStyle", "NSTableView.RowSizeStyle" },
			{ "AppKit.NSTableViewSelectionHighlightStyle", "NSTableView.SelectionHighlightStyle" },
			{ "AppKit.NSTabPosition", "NSTabView.TabPosition" },
			{ "AppKit.NSTabState", "NSTabViewItem.State" },
			{ "AppKit.NSTabViewBorderType", "NSTabView.TabViewBorderType" },
			{ "AppKit.NSTabViewControllerTabStyle", "NSTabViewController.TabStyle" },
			{ "AppKit.NSTabViewType", "NSTabView.TabType" },
			{ "AppKit.NSTextBlockDimension", "NSTextBlock.Dimension" },
			{ "AppKit.NSTextBlockLayer", "NSTextBlock.Layer" },
			{ "AppKit.NSTextBlockValueType", "NSTextBlock.ValueType" },
			{ "AppKit.NSTextBlockVerticalAlignment", "NSTextBlock.VerticalAlignment" },
			{ "AppKit.NSTextFieldBezelStyle", "NSTextField.BezelStyle" },
			{ "AppKit.NSTextFinderAction", "NSTextFinder.Action" },
			{ "AppKit.NSTextFinderMatchingType", "NSTextFinder.MatchingType" },
			{ "AppKit.NSTextLayoutOrientation", "NSLayoutManager.TextLayoutOrientation" },
			{ "AppKit.NSTextListOptions", "NSTextList.Options" },
			{ "AppKit.NSTextTableLayoutAlgorithm", "NSTextTable.LayoutAlgorithm" },
			{ "AppKit.NSTextTabType", "NSParagraphStyle.TextTabType" },
			{ "AppKit.NSTickMarkPosition", "NSSlider.TickMarkPosition" },
			{ "AppKit.NSTiffCompression", "NSBitmapImageRep.TIFFCompression" },
			{ "AppKit.NSTitlePosition", "NSBox.TitlePosition" },
			{ "AppKit.NSTokenStyle", "NSTokenField.TokenStyle" },
			{ "AppKit.NSToolbarDisplayMode", "NSToolbar.DisplayMode" },
			{ "AppKit.NSToolbarItemGroupControlRepresentation", "NSToolbarItemGroup.ControlRepresentation" },
			{ "AppKit.NSToolbarItemGroupSelectionMode", "NSToolbarItemGroup.SelectionMode" },
			{ "AppKit.NSToolbarSizeMode", "NSToolbar.SizeMode" },
			{ "AppKit.NSTouchBarItemIdentifier", "NSTouchBarItem.Identifier" },
			{ "AppKit.NSTouchPhase", "NSTouch.Phase" },
			{ "AppKit.NSTouchType", "NSTouch.TouchType" },
			{ "AppKit.NSTouchTypeMask", "NSTouch.TouchTypeMask" },
			{ "AppKit.NSTrackingAreaOptions", "NSTrackingArea.Options" },
			{ "AppKit.NSTypesetterBehavior", "NSLayoutManager.TypesetterBehavior" },
			{ "AppKit.NSUsableScrollerParts", "NSScroller.UsableParts" },
			{ "AppKit.NSViewControllerTransitionOptions", "NSViewController.TransitionOptions" },
			{ "AppKit.NSViewLayerContentsPlacement", "NSView.LayerContentsPlacement" },
			{ "AppKit.NSViewLayerContentsRedrawPolicy", "NSView.LayerContentsRedrawPolicy" },
			{ "AppKit.NSViewResizingMask", "NSView.AutoresizingMask" },
			{ "AppKit.NSVisualEffectBlendingMode", "NSVisualEffectView.BlendingMode" },
			{ "AppKit.NSVisualEffectMaterial", "NSVisualEffectView.Material" },
			{ "AppKit.NSVisualEffectState", "NSVisualEffectView.State" },
			{ "AppKit.NSWindingRule", "NSBezierPath.WindingRule" },
			{ "AppKit.NSWindowAnimationBehavior", "NSWindow.AnimationBehavior" },
			{ "AppKit.NSWindowBackingLocation", "NSWindow.BackingLocation" },
			{ "AppKit.NSWindowButton", "NSWindow.ButtonType" },
			{ "AppKit.NSWindowCollectionBehavior", "NSWindow.CollectionBehavior" },
			{ "AppKit.NSWindowDepth", "NSWindow.Depth" },
			{ "AppKit.NSWindowLevel", "NSWindow.Level" },
			{ "AppKit.NSWindowListOptions", "NSApplication.WindowListOptions" },
			{ "AppKit.NSWindowNumberListOptions", "NSWindow.NumberListOptions" },
			{ "AppKit.NSWindowOcclusionState", "NSWindow.OcclusionState" },
			{ "AppKit.NSWindowOrderingMode", "NSWindow.OrderingMode" },
			{ "AppKit.NSWindowSharingType", "NSWindow.SharingType" },
			{ "AppKit.NSWindowStyle", "NSWindow.StyleMask" },
			{ "AppKit.NSWindowTabbingMode", "NSWindow.TabbingMode" },
			{ "AppKit.NSWindowTitleVisibility", "NSWindow.TitleVisibility" },
			{ "AppKit.NSWindowUserTabbingPreference", "NSWindow.UserTabbingPreference" },
			{ "AppKit.NSWorkspaceAuthorizationType", "NSWorkspace.AuthorizationType" },
			{ "AppKit.NSWorkspaceIconCreationOptions", "NSWorkspace.IconCreationOptions" },
			{ "AppKit.NSWorkspaceLaunchOptions", "NSWorkspace.LaunchOptions" },
			// AuthenticationServices
			{ "AuthenticationServices.ASAuthorizationScope", "ASAuthorization.Scope" },
			// AVFoundation
			{ "AVFoundation.AVSampleBufferRequestDirection", "AVSampleBufferRequest.Direction" },
			{ "AVFoundation.AVSampleBufferRequestMode", "AVSampleBufferRequest.Mode" },
			{ "AVFoundation.AVSemanticSegmentationMatteType", "AVSemanticSegmentationMatte.MatteType" },
			// AVKit
			{ "AVKit.AVRoutePickerViewButtonState", "AVRoutePickerView.ButtonState" },
			// CoreServices
			{ "CoreServices.LSRoles", "LSRolesMask" },
			// Foundation
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
			{ "Foundation.NSDataCompressionAlgorithm", "NSData.CompressionAlgorithm" },
			{ "Foundation.NSDataWritingOptions", "NSData.WritingOptions" },
			{ "Foundation.NSDataSearchOptions", "NSData.SearchOptions" },
			{ "Foundation.NSDateComponentsFormatterUnitsStyle", "DateComponentsFormatter.UnitsStyle" },
			{ "Foundation.NSDateFormatterBehavior", "DateFormatter.Behavior" },
			{ "Foundation.NSDateFormatterStyle", "DateFormatter.Style" },
			{ "Foundation.NSRelativeDateTimeFormatterUnitsStyle", "RelativeDateTimeFormatter.UnitsStyle" },
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
			{ "Foundation.NSRelativeDateTimeFormatterStyle", "RelativeDateTimeFormatter.DateTimeStyle" },
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
			// MapKit
			{ "MapKit.MKLocalSearchCompleterResultType", "MKLocalSearchCompleter.ResultType" },
			{ "MapKit.MKLocalSearchResultType", "MKLocalSearch.ResultType" },
			// NetworkExtension
			{ "NetworkExtension.NEFilterReportFrequency", "NEFilterReport.Frequency" },
			{ "NetworkExtension.NEFilterManagerGrade", "NEFilterManager.Grade" },
			{ "NetworkExtension.NEFilterPacketProviderVerdict", "NEFilterPacketProvider.Verdict" },
			{ "NetworkExtension.NEFilterReportEvent", "NEFilterReport.Event" },
			// Photos
			{ "Photos.PHLivePhotoEditingError", "PHLivePhotoEditingErrorCode" },
			{ "Photos.PHProjectCreationSource", "PHProjectInfo.CreationSource" },
			{ "Photos.PHProjectSectionType", "PHProjectSection.SectionType" },
			{ "Photos.PHProjectTextElementType", "PHProjectTextElement.ElementType" },
			// QuickLook
			{ "QuickLookThumbnailing.QLThumbnailGenerationRequestRepresentationTypes", "QLThumbnailGenerator.Request.RepresentationTypes" },
			{ "QuickLookThumbnailing.QLThumbnailRepresentationType", "QLThumbnailRepresentation.RepresentationType" },
			// StoreKit
			{ "StoreKit.SKProductDiscountType", "SKProductDiscount.Type" },
			// WebKit
			{ "WebKit.WKContentMode", "WKWebpagePreferences.ContentMode" },
		};
	}
}
