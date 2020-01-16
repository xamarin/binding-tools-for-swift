// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace SwiftReflector.Importing {
	public partial class TypeAggregator {

		static partial void ModulesToSkipIOS (ref HashSet<string> result) { result = iOSModulesToSkip; }
		static HashSet<string> iOSModulesToSkip = new HashSet<string> () {
			"OpenTK",
	    		"CoreMidi",
			"Registrar",
	    		"Twitter",
			"CoreAnimation",
			"CoreServices",
	    		"System",
			"System.Drawing",
	    		"Xamarin.Utils",
			"ObjCRuntime",
	    		"VideoSubscriberAccount", // tvOS only
		};

		static partial void AvailableMapIOS (ref Dictionary<string, string> result)
		{
			result = iOSAvailableMap;
		}

		static Dictionary<string, String> iOSAvailableMap = new Dictionary<string, string> () {
		};

		static partial void TypesToSkipIOS (ref HashSet<string> result) { result = iOSTypesToSkip; }
		static HashSet<string> iOSTypesToSkip = new HashSet<string> () {
			// Accelerate
			"Accelerate.vImageGamma", // not an enum
			// Accounts
			"Accounts.ACFacebookAudience",
			// AddressBook
			"AddressBook.ABAddressBookError", // not an enum
			"AddressBook.ABPersonKind", // not an enum
			"AddressBook.ABPersonProperty", // not an enum
			"AddressBook.ABPersonSortBy", // not an enum
			"AddressBook.ABSourceProperty", // not an enum
			// ARKit
			"ARKit.ARPlaneClassificationStatus",
			// AssetsLibrary
			"AssetsLibrary.ALAssetsError", // not an enum
			"AssetsLibrary.ALAssetType", // not an enum
			// AudioToolbox
			"AudioToolbox.AudioCodecComponentType", // not an enum
			"AudioToolbox.AudioConverterError", // not an enum
			"AudioToolbox.AudioConverterPrimeMethod", // not an enum
			"AudioToolbox.AudioConverterQuality", // not an enum
			"AudioToolbox.AudioConverterSampleRateConverterComplexity", // not an enum
			"AudioToolbox.AudioFileChunkType", // can't find it
			"AudioToolbox.AudioFileError", // not an enum
			"AudioToolbox.AudioFileLoopDirection", // not an enum
			"AudioToolbox.AudioFileMarkerType", // not an enum
			"AudioToolbox.AudioFileProperty", // not an enum
			"AudioToolbox.AudioFileStreamProperty", // not an enum
			"AudioToolbox.AudioFileStreamStatus", // can't find it
			"AudioToolbox.AudioFileType", // not an enum
			"AudioToolbox.AudioFormatError", // not an enum
			"AudioToolbox.AudioFormatType", // can't find it
			"AudioToolbox.AudioQueueDeviceProperty", // not an enum
			"AudioToolbox.AudioQueueHardwareCodecPolicy", // not an enum
			"AudioToolbox.AudioQueueParameter", // not an enum
			"AudioToolbox.AudioQueueProperty", // not an enum
			"AudioToolbox.AudioQueueStatus", // can't find it
			"AudioToolbox.AudioQueueTimePitchAlgorithm", // not an enum
			"AudioToolbox.AudioServicesError", // can't find it
			"AudioToolbox.AudioSessionActiveFlags", // not an enum
			"AudioToolbox.AudioSessionCategory", // not an enum
			"AudioToolbox.AudioSessionErrors", // not an enum
			"AudioToolbox.AudioSessionInputRouteKind", // not an enum, not in Swift
			"AudioToolbox.AudioSessionInterruptionState", // not an enum
			"AudioToolbox.AudioSessionMode", // not an enum
			"AudioToolbox.AudioSessionOutputRouteKind", // not an enum, not in Swift
			"AudioToolbox.AudioSessionProperty", // not an enum
			"AudioToolbox.AudioSessionRouteChangeReason", // not an enum
			"AudioToolbox.AudioSessionRoutingOverride", // not an enum
			"AudioToolbox.MusicPlayerStatus", // can't find it
			"AudioToolbox.SmpteTime", // wrong namespace
			"AudioToolbox.SmpteTimeFlags", // wrong namespace
			"AudioToolbox.SmpteTimeType", // wrong namespace
			// AudioUnit
			"AudioUnit.AudioCodecManufacturer", // not an enum
			"AudioUnit.AudioComponentManufacturerType", // can't find it
			"AudioUnit.AudioComponentStatus", // can't find it
			"AudioUnit.AudioComponentType", // can't find it
			"AudioUnit.AudioObjectPropertyElement", // not an enum
			"AudioUnit.AudioObjectPropertyScope", // wrong namespace
			"AudioUnit.AudioObjectPropertySelector", // wrong namespace
			"AudioUnit.AudioTypeConverter", // not an enum
			"AudioUnit.AudioTypeEffect", // not an enum
			"AudioUnit.AudioTypeGenerator", // not an enum
			"AudioUnit.AudioTypeMixer", // not an enum
			"AudioUnit.AudioTypeMusicDevice", // not an enum
			"AudioUnit.AudioTypeOutput", // not an enum
			"AudioUnit.AudioTypePanner", // unimplemented
			"AudioUnit.AudioUnitClumpID", // not an enum
			"AudioUnit.AudioUnitParameterType", // can't find it
			"AudioUnit.AudioUnitPropertyIDType", // not an enum
			"AudioUnit.AudioUnitScopeType", // not an enum
			"AudioUnit.AudioUnitStatus", // can't find it
			"AudioUnit.AudioUnitSubType", // not an enum
			"AudioUnit.AUGraphError", // not an enum
			"AudioUnit.ExtAudioFileError", // not an enum
			"AudioUnit.InstrumentType", // not an enum
			// AVFoundation
			"AVFoundation.AVAssetExportSessionPreset", // not an enum
			"AVFoundation.AVAudioBitRateStrategy", // not an enum
			"AVFoundation.AVAudioDataSourceLocation", // not an enum
			"AVFoundation.AVAudioDataSourceOrientation", // not an enum
			"AVFoundation.AVAudioDataSourcePolarPattern", // not an enum
			"AVFoundation.AVAudioSessionFlags", // not an enum
			"AVFoundation.AVAudioSessionInterruptionFlags", // not an enum
			"AVFoundation.AVAuthorizationMediaType", // can't find it
			"AVFoundation.AVCaptureDeviceTransportControlsPlaybackMode", // marked unavailable
			"AVFoundation.AVMediaTypes", // not an enum
			"AVFoundation.AVSampleCursorChunkInfo", // Mac only
			"AVFoundation.AVSampleCursorDependencyInfo", // Mac only
			"AVFoundation.AVSampleCursorStorageRange", // Mac only
			"AVFoundation.AVSampleCursorSyncInfo", // Mac only
			"AVFoundation.AVSampleRateConverterAlgorithm", // not an enum
			"AVFoundation.AVVideoCodec", // not an enum
			"AVFoundation.AVVideoFieldMode", // marked unavailable
			"AVFoundation.AVVideoH264EntropyMode", // not an enum
			"AVFoundation.AVVideoProfileLevelH264", // not an enum
			"AVFoundation.AVVideoScalingMode", // not an enum
			// AVKit
			"AVKit.AVPlayerViewControlsStyle", // Mac only
			// CallKit
			"CallKit.CXErrorCode", // doesn't exist in Swift
			// Contacts
			"Contacts.CNContactOptions", // not an enum
			"Contacts.CNInstantMessageAddressOption", // not an enum
			"Contacts.CNInstantMessageServiceOption", // not an enum
			"Contacts.CNPostalAddressKeyOption", // not an enum
			"Contacts.CNSocialProfileOption", // not an enum
			"Contacts.CNSocialProfileServiceOption", // not an enum
			// CoreData
			"CoreData.MigrationErrorType", // not an enum
			"CoreData.ObjectGraphManagementErrorType", // not an enum
			"CoreData.PersistentStoreErrorType", // not an enum
			"CoreData.ValidationErrorType", // not an enum
			// CoreFoundation
			"CoreFoundation.CFMessagePortSendRequestStatus", // not an enum
			"CoreFoundation.CFProxyType", // not an enum
			"CoreFoundation.CFSocketFlags", // no name
			"CoreFoundation.DispatchQueuePriority", // not an enum
			"CoreFoundation.MemoryPressureFlags", // not an enum
			"CoreFoundation.ProcessMonitorFlags", // not an enum
			"CoreFoundation.VnodeMonitorKind", // not an enum
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
			// EventKit
			"EventKit.EKCalendarEventAvailability", // not an enum
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
			// GameKit
			"GameKit.GKGameSessionErrorCode", // no longer exists
			"GameKit.GKPeerConnectionState", // marked unavailable
			"GameKit.GKPeerPickerConnectionType", // marked unavailable
			"GameKit.GKSendDataMode", // marked unavailable
			"GameKit.GKSessionMode", // marked unavailable
			"GameKit.GKVoiceChatServiceError", // marked unavailable
			// HomeKit
			"HomeKit.HMAccessoryCategoryType", // not an enum
			"HomeKit.HMActionSetType", // not an enum
			"HomeKit.HMCharacteristicMetadataFormat", // not an enum
			"HomeKit.HMCharacteristicMetadataUnits", // not an enum
			"HomeKit.HMCharacteristicType", // not an enum
			"HomeKit.HMServiceType", // not an enum
			// IdentityLookup
			"IdentityLookup.ILClassificationAction",
			// ImageIO
			"ImageIO.CGImageAuxiliaryDataType", // not an enum
			"ImageIO.CGImagePropertyPngFilters", // can't find it
			// Intents
			"Intents.INIntentIdentifier", // not an enum
			"Intents.INPriceRangeOption", // not an enum
			// IntentsUI
	    		"IntentsUI.INUIAddVoiceShortcutButton",
			"IntentsUI.INUIAddVoiceShortcutButtonStyle",
			// IOSurface
			"IOSurface.IOSurfaceMemoryMap", // has become anonymous
			// MapKit
			"MapKit.MKDirectionsMode", // not an enum
			// MediaPlayer
			"MediaPlayer.MPMovieControlMode", // not an enum
			// MediaToolbox
			"MediaToolbox.MTAudioProcessingTapError", // can't find it
			// Messages
			"Messages.MSMessagesAppPresentationContext",
			// Metal
			"Metal.MTLClearValue", // can't find it
			"Metal.MTLRenderPipelineError", // can't find it
			"Metal.MTLSamplerBorderColor", // marked unavailable
			// ModelIO
			"ModelIO.MDLNoiseTextureType", // can't find it
			"ModelIO.MDLVoxelIndexExtent", // replaced
			// Network
			"Network.NWEndpointType", // not an enum
			"Network.NWErrorDomain", // can't find it
			"Network.NWMultiPathService", // can't find it
			"Network.NWConnectionState", // iOS 12.0 or later
			"Network.NWInterfaceType", // iOS 12.0 or later
			"Network.NWListenerState", // iOS 12.0 or later
			// PassKit
			"PassKit.PKErrorCode", // does not exist
			// PdfKit
			"PdfKit.PdfPrintScalingMode", // macOS only
			// SafariServices
			"SafariServices.SFSafariViewControllerDismissButtonStyle",
			// SceneKit
			"SceneKit.SCNErrorCode", // can't find it
			"SceneKit.SCNTessellationSmoothingMode", // can't find it
			// Security
			"Security.SecAccessible", // not an enum
			"Security.SecAuthenticationType", // Mac only
			"Security.SecAuthenticationUI", // not an enum
			"Security.SecKeyClass", // not an enum
			"Security.SecKeyType", // not an enum
			"Security.SecKind", // can't find it
			"Security.SecProtocol", // Mac only (SecProtocolType)
			"Security.SecRevocation", // not an enum
			"Security.SecStatusCode", // can't find it
			"Security.SecTokenID", // not an enum
			"Security.SslSessionConfig", // can't find it
			"Security.SslSessionStrengthPolicy", // can't find it
			"Security.SslStatus", // can't find it
			// Social
			"Social.SLServiceKind", // not an enum
			// SystemConfiguration
			"SystemConfiguration.StatusCode", // not an enum
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
			"VideoToolbox.VTDataRateLimit", // can't find it
			"VideoToolbox.VTTransferFunction", // can't find it
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
			// Accelerate
			{ "Accelerate.Pixel8888", "Pixel_8888" },
			{ "Accelerate.PixelARGB16S", "Pixel_ARGB_16S" },
			{ "Accelerate.PixelARGB16U", "Pixel_ARGB_16U" },
			{ "Accelerate.PixelFFFF", "Pixel_FFFF" },
			{ "Accelerate.vImageAffineTransformDouble", "vImage_AffineTransform_Double" },
			{ "Accelerate.vImageAffineTransformFloat", "vImage_AffineTransform" },
			{ "Accelerate.vImageBuffer", "vImage_Buffer" },
			{ "Accelerate.vImageError", "vImage_Error" },
			{ "Accelerate.vImageFlags", "vImage_Flags" },
			{ "Accelerate.vImageInterpolationMethod", "vImage_InterpolationMethod" },
			// ARKit
			{ "ARKit.AREnvironmentTexturing", "ARWorldTrackingConfiguration.EnvironmentTexturing" },
			{ "ARKit.ARErrorCode", "ARError.Code" },
			{ "ARKit.ARHitTestResultType", "ARHitTestResult.ResultType" },
			{ "ARKit.ARPlaneAnchorAlignment", "ARPlaneAnchor.Alignment" },
			{ "ARKit.ARPlaneClassification", "ARKit.ARPlaneAnchor.Classification" },
			{ "ARKit.ARPlaneDetection", "ARKit.ARWorldTrackingConfiguration.PlaneDetection" },
			{ "ARKit.ARSessionRunOptions", "ARSession.RunOptions" },
			{ "ARKit.ARTrackingState", "ARCamera.TrackingState" },
			{ "ARKit.ARTrackingStateReason", "ARCamera.TrackingState.Reason" },
			{ "ARKit.ARWorldAlignment", "ARConfiguration.WorldAlignment" },
			{ "ARKit.ARWorldMappingStatus", "ARFrame.WorldMappingStatus" },
			// AudioToolbox
			{ "AudioToolbox.AudioChannelBit", "AudioChannelBitmap" },
			{ "AudioToolbox.AudioFilePermission", "AudioFilePermissions" },
			{ "AudioToolbox.AudioFileSmpteTime", "AudioFile_SMPTE_Time" },
			{ "AudioToolbox.AudioFileStreamPropertyFlag", "AudioFileStreamPropertyFlags" },
			{ "AudioToolbox.AudioFormat", "AudioFormatListItem" },
			{ "AudioToolbox.MidiChannelMessage", "MIDIChannelMessage" },
			{ "AudioToolbox.MidiNoteMessage", "MIDINoteMessage" },
			{ "AudioToolbox.PanningMode", "AudioPanningMode" },
			// AudioUnit
			{ "AudioUnit.AudioComponentFlag", "AudioComponentFlags" },
			{ "AudioUnit.AudioUnitBusType", "AUAudioUnitBusType" },
			{ "AudioUnit.AudioUnitParameterFlag", "AudioUnitParameterOptions" },
			{ "AudioUnit.ScheduledAudioSliceFlag", "AUScheduledAudioSliceFlags" },
			{ "AudioUnit.SpatialMixerAttenuation", "AUSpatialMixerAttenuationCurve" },
			{ "AudioUnit.SpatialMixerRenderingFlags", "AUSpatialMixerRenderingFlags" },
			// AuthenticationServices
			{ "AuthenticationServices.ASCredentialIdentityStoreErrorCode", "ASCredentialIdentityStoreError.Code" },
			{ "AuthenticationServices.ASCredentialServiceIdentifierType", "ASCredentialServiceIdentifier.IdentifierType" },
			{ "AuthenticationServices.ASExtensionErrorCode", "ASExtensionError.Code" },
			{ "AuthenticationServices.ASWebAuthenticationSessionErrorCode", "ASWebAuthenticationSessionError.Code" },
			// AVFoundation
			{ "AVFoundation.AVAssetExportSessionStatus", "AVAssetExportSession.Status" },
			{ "AVFoundation.AVAssetImageGeneratorResult", "AVAssetImageGenerator.Result" },
			{ "AVFoundation.AVAssetReaderStatus", "AVAssetReader.Status" },
			{ "AVFoundation.AVAssetWriterInputMediaDataLocation", "AVAssetWriterInput.MediaDataLocation" },
			{ "AVFoundation.AVAssetWriterStatus", "AVAssetWriter.Status" },
			{ "AVFoundation.AVAudioSessionCategory", "AVAudioSession.Category" },
			{ "AVFoundation.AVAudioSessionCategoryOptions", "AVAudioSession.CategoryOptions" },
			{ "AVFoundation.AVAudioSessionErrorCode", "AVAudioSession.ErrorCode" },
			{ "AVFoundation.AVAudioSessionInterruptionOptions", "AVAudioSession.InterruptionOptions" },
			{ "AVFoundation.AVAudioSessionInterruptionType", "AVAudioSession.InterruptionType" },
			{ "AVFoundation.AVAudioSessionIOType", "AVAudioSession.IOType" },
			{ "AVFoundation.AVAudioSessionPortOverride", "AVAudioSession.PortOverride" },
			{ "AVFoundation.AVAudioSessionRecordPermission", "AVAudioSession.RecordPermission" },
			{ "AVFoundation.AVAudioSessionRouteChangeReason", "AVAudioSession.RouteChangeReason" },
			{ "AVFoundation.AVAudioSessionRouteSharingPolicy", "AVAudioSession.RouteSharingPolicy" },
			{ "AVFoundation.AVAudioSessionSetActiveOptions", "AVAudioSession.SetActiveOptions" },
			{ "AVFoundation.AVAudioSessionSilenceSecondaryAudioHintType", "AVAudioSession.SilenceSecondaryAudioHintType" },
			{ "AVFoundation.AVCaptureAutoFocusRangeRestriction", "AVCaptureDevice.AutoFocusRangeRestriction" },
			{ "AVFoundation.AVCaptureAutoFocusSystem", "AVCaptureDevice.Format.AutoFocusSystem" },
			{ "AVFoundation.AVCaptureDevicePosition", "AVCaptureDevice.Position" },
			{ "AVFoundation.AVCaptureDeviceType", "AVCaptureDevice.DeviceType" },
			{ "AVFoundation.AVCaptureDeviceFormat", "AVCaptureDevice.Format" },
			{ "AVFoundation.AVCaptureExposureMode", "AVCaptureDevice.ExposureMode" },
			{ "AVFoundation.AVCaptureFlashMode", "AVCaptureDevice.FlashMode" },
			{ "AVFoundation.AVCaptureFocusMode", "AVCaptureDevice.FocusMode" },
			{ "AVFoundation.AVCaptureLensStabilizationStatus", "AVCaptureDevice.LensStabilizationStatus" },
			{ "AVFoundation.AVCaptureOutputDataDroppedReason", "AVCaptureOutput.DataDroppedReason" },
			{ "AVFoundation.AVCaptureSessionInterruptionReason", "AVCaptureSession.InterruptionReason" },
			{ "AVFoundation.AVCaptureSystemPressureFactors", "AVCaptureDevice.SystemPressureState.Factors" },
			{ "AVFoundation.AVCaptureSystemPressureLevel", "AVCaptureDevice.SystemPressureState.Level" },
			{ "AVFoundation.AVCaptureSystemPressureState", "AVCaptureDevice.SystemPressureState" },
			{ "AVFoundation.AVCaptureTorchMode", "AVCaptureDevice.TorchMode" },
			{ "AVFoundation.AVCaptureWhiteBalanceChromaticityValues", "AVCaptureDevice.WhiteBalanceChromaticityValues" },
			{ "AVFoundation.AVCaptureWhiteBalanceGains", "AVCaptureDevice.WhiteBalanceGains" },
			{ "AVFoundation.AVCaptureWhiteBalanceMode", "AVCaptureDevice.WhiteBalanceMode" },
			{ "AVFoundation.AVCaptureWhiteBalanceTemperatureAndTintValues", "AVCaptureDevice.WhiteBalanceTemperatureAndTintValues" },
			{ "AVFoundation.AVContentKeyRequestRetryReason", "AVContentKeyRequest.RetryReason" },
			{ "AVFoundation.AVContentKeyRequestStatus", "AVContentKeyRequest.Status" },
			{ "AVFoundation.AVDepthDataAccuracy", "AVDepthData.Accuracy" },
			{ "AVFoundation.AVDepthDataQuality", "AVDepthData.Quality" },
			{ "AVFoundation.AVFileTypes", "AVFileType" },
			{ "AVFoundation.AVMediaCharacteristics", "AVMediaCharacteristic" },
			{ "AVFoundation.AVMetadataObjectType", "AVMetadataObject.ObjectType" },
			{ "AVFoundation.AVPlayerActionAtItemEnd", "AVPlayer.ActionAtItemEnd" },
			{ "AVFoundation.AVPlayerHdrMode", "AVPlayer.HDRMode" },
			{ "AVFoundation.AVPlayerItemStatus", "AVPlayerItem.Status" },
			{ "AVFoundation.AVPlayerLooperStatus", "AVPlayerLooper.Status" },
			{ "AVFoundation.AVPlayerStatus", "AVPlayer.Status" },
			{ "AVFoundation.AVPlayerTimeControlStatus", "AVPlayer.TimeControlStatus" },
			// BusinessChat
			{ "BusinessChat.BCChatButtonStyle", "BCChatButton.Style" },
			{ "BusinessChat.BCParameterName", "BCChatAction.Parameter" },
			// CallKit
			{ "CallKit.CXCallDirectoryEnabledStatus", "CXCallDirectoryManager.EnabledStatus" },
			{ "CallKit.CXHandleType", "CXHandle.HandleType" },
			{ "CallKit.CXPlayDtmfCallActionType", "CXPlayDTMFCallAction.ActionType" },
			// CarPlay
			{ "CarPlay.CPAlertActionStyle", "CPAlertAction.Style" },
			{ "CarPlay.CPBarButtonType", "CPBarButton.Type" },
			{ "CarPlay.CPNavigationAlertDismissalContext", "CPNavigationAlert.DismissalContext" },
			{ "CarPlay.CPPanDirection", "CPMapTemplate.PanDirection" },
			{ "CarPlay.CPTripPauseReason", "CPNavigationSession.PauseReason" },
			// ClassKit
			{ "ClassKit.CLSErrorCode", "CLSError.Code" },
			// CloudKit
			{ "CloudKit.CKApplicationPermissions", "CKContainer_Application_Permissions" },
			{ "CloudKit.CKApplicationPermissionStatus", "CKContainer_Application_PermissionStatus" },
			{ "CloudKit.CKDatabaseScope", "CKDatabase.Scope" },
			{ "CloudKit.CKErrorCode", "CKError.Code" },
			{ "CloudKit.CKNotificationType", "CKNotification.NotificationType" },
			{ "CloudKit.CKOperationGroupTransferSize", "CKOperationGroup.TransferSize" },
			{ "CloudKit.CKQueryNotificationReason", "CKQueryNotification.Reason" },
			{ "CloudKit.CKQuerySubscriptionOptions", "CKQuerySubscription.Options" },
			{ "CloudKit.CKRecordSavePolicy", "CKModifyRecordsOperation.RecordSavePolicy" },
			{ "CloudKit.CKRecordZoneCapabilities", "CKRecordZone.Capabilities" },
			{ "CloudKit.CKReferenceAction", "CKRecord_Reference_Action" },
			{ "CloudKit.CKShareParticipantAcceptanceStatus", "CKShare_Participant_AcceptanceStatus" },
			{ "CloudKit.CKShareParticipantPermission", "CKShare_Participant_Permission" },
			{ "CloudKit.CKShareParticipantRole", "CKShare_Participant_Role" },
			{ "CloudKit.CKShareParticipantType", "CKShare_Participant_ParticipantType" },
			{ "CloudKit.CKSubscriptionType", "CKSubscription.SubscriptionType" },
			// Compression
			{ "Compression.CompressionAlgorithm", "compression_algorithm" },
			// Contacts
			{ "Contacts.CNErrorCode", "CNError.Code" },
			// CoreFoundation
			{ "CoreFoundation.CFRunLoopExitReason", "CFRunLoopRunResult" },
			{ "CoreFoundation.CFUrlPathStyle", "CFURLPathStyle" },
			// EventKit
			{ "EventKit.EKDay", "EKWeekday" },
			{ "EventKit.EKErrorCode", "EKError.Code" },
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
			// GameKit
			{ "GameKit.GKLeaderboardPlayerScope", "GKLeaderboard.PlayerScope" },
			{ "GameKit.GKLeaderboardTimeScope", "GKLeaderboard.TimeScope" },
			{ "GameKit.GKMatchSendDataMode", "GKMatch.SendDataMode" },
			{ "GameKit.GKPhotoSize", "GKPlayer.PhotoSize" },
			{ "GameKit.GKTurnBasedMatchOutcome", "GKTurnBasedMatch.Outcome" },
			{ "GameKit.GKTurnBasedMatchStatus", "GKTurnBasedMatch.Status" },
			{ "GameKit.GKTurnBasedParticipantStatus", "GKTurnBasedParticipant.Status" },
			{ "GameKit.GKVoiceChatPlayerState", "GKVoiceChat.PlayerState" },
			// HealthKit
			{ "HealthKit.HKErrorCode", "HKError.Code" },
			{ "HealthKit.HKFhirResourceType", "HKFHIRResourceType" },
			// HomeKit
			{ "HomeKit.HMCharacteristicValueAirParticulate", "HMCharacteristicValueAirParticulateSize" },
			{ "HomeKit.HMCharacteristicValueLockMechanism", "HMCharacteristicValueLockMechanismLastKnownAction" },
			// Intents
			{ "Intents.INDailyRoutineSituation", "INDailyRoutineRelevanceProvider.Situation" },
			{ "Intents.INIntentErrorCode", "INIntentError.Code" },
			// LocalAuthentication
			{ "LocalAuthentication.LAStatus", "LAError" },
			// MapKit
			{ "MapKit.MKAnnotationViewCollisionMode", "MKAnnotationView.CollisionMode" },
			{ "MapKit.MKAnnotationViewDragState", "MKAnnotationView.DragState" },
			{ "MapKit.MKDistanceFormatterUnits", "MKDistanceFormatter.Units" },
			{ "MapKit.MKDistanceFormatterUnitStyle", "MKDistanceFormatter.DistanceUnitStyle" },
			{ "MapKit.MKErrorCode", "MKError.Code" },
			{ "MapKit.MKScaleViewAlignment", "MKScaleView.Alignment" },
			{ "MapKit.MKSearchCompletionFilterType", "MKLocalSearchCompleter.FilterType" },
			// MediaPlayer
			{ "MediaPlayer.MPErrorCode", "MPError.Code" },
			{ "MediaPlayer.MPMovieMediaType", "MPMovieMediaTypeMask" },
			// MessageUI
			{ "MessageUI.MFMailComposeErrorCode", "MFMailComposeError.Code" },
			// Metal
			{ "Metal.MTLCpuCacheMode", "MTLCPUCacheMode" },
			// MetalKit
			{ "MetalKit.MTKTextureLoaderCubeLayout", "MTKTextureLoader.CubeLayout" },
			{ "MetalKit.MTKTextureLoaderOrigin", "MTKTextureLoader.Origin" },
			// MetalPerformanceShaders
			{ "MetalPerformanceShaders.MPSCnnBinaryConvolutionFlags", "MPSCNNBinaryConvolutionFlags" },
			{ "MetalPerformanceShaders.MPSCnnBinaryConvolutionType", "MPSCNNBinaryConvolutionType" },
			{ "MetalPerformanceShaders.MPSCnnConvolutionFlags", "MPSCNNConvolutionFlags" },
			{ "MetalPerformanceShaders.MPSCnnNeuronType", "MPSCNNNeuronType" },
			{ "MetalPerformanceShaders.MPSRnnBidirectionalCombineMode", "MPSRNNBidirectionalCombineMode" },
			{ "MetalPerformanceShaders.MPSRnnSequenceDirection", "MPSRNNSequenceDirection" },
			// ModelIO
			{ "ModelIO.MDLVoxelIndexExtent2", "MDLVoxelIndexExtent" },
			// NaturalLanguage
			{ "NaturalLanguage.NLModelType", "NLModel.ModelType" },
			{ "NaturalLanguage.NLTaggerOptions", "NLTagger.Options" },
			{ "NaturalLanguage.NLTokenizerAttributes", "NLTokenizer.Attributes" },
			// Network
			// iOS 12.0 or later: { "Network.NWConnectionState", "NWConnection.State" },
			// iOS 12.0 or later: { "Network.NWInterfaceType", "NWInterface.InterfaceType" },
			{ "Network.NWIPEcnFlag", "nw_ip_ecn_flag_t" },
			{ "Network.NWIPVersion", "nw_ip_version_t" },
			// iOS 12.0 or later: { "Network.NWListenerState", "NWListener.State" },
			{ "Network.NWParametersExpiredDnsBehavior", "NWParameters.ExpiredDNSBehavior" },
			{ "Network.NWServiceClass", "nw_service_class_t" },
			// NetworkExtension
			{ "NetworkExtension.NEDnsProxyManagerError", "NEDNSProxyManagerError" },
			{ "NetworkExtension.NEHotspotConfigurationEapTlsVersion", "NEHotspotEAPSettings.TLSVersion" },
			{ "NetworkExtension.NEHotspotConfigurationEapType", "NEHotspotEAPSettings.EAPType" },
			{ "NetworkExtension.NEHotspotConfigurationTtlsInnerAuthenticationType", "NEHotspotEAPSettings.TTLSInnerAuthenticationType" },
			{ "NetworkExtension.NEVpnError", "NEVPNError" },
			{ "NetworkExtension.NEVpnIke2CertificateType", "NEVPNIKEv2CertificateType" },
			{ "NetworkExtension.NEVpnIke2DeadPeerDetectionRate", "NEVPNIKEv2DeadPeerDetectionRate" },
			{ "NetworkExtension.NEVpnIke2DiffieHellman", "NEVPNIKEv2DiffieHellmanGroup" },
			{ "NetworkExtension.NEVpnIke2EncryptionAlgorithm", "NEVPNIKEv2EncryptionAlgorithm" },
			{ "NetworkExtension.NEVpnIke2IntegrityAlgorithm", "NEVPNIKEv2IntegrityAlgorithm" },
			{ "NetworkExtension.NEVpnIkeAuthenticationMethod", "NEVPNIKEAuthenticationMethod" },
			{ "NetworkExtension.NEVpnIkev2TlsVersion", "NEVPNIKEv2TLSVersion" },
			{ "NetworkExtension.NEVpnStatus", "NEVPNStatus" },
			{ "NetworkExtension.NWTcpConnectionState", "NWTCPConnectionState" },
			{ "NetworkExtension.NWUdpSessionState", "NWUDPSessionState" },
			// PassKit
			{ "PassKit.PKContactFields", "PKContactField" },
			{ "PassKit.PKPassKitErrorCode", "PKPassKitError.Code" },
			{ "PassKit.PKPaymentErrorCode", "PKPaymentError.Code" },
			// PdfKit
			{ "PDFKit.PdfActionNamedName", "PDFActionNamedName" },
			{ "PDFKit.PdfAnnotationHighlightingMode", "PDFAnnotationHighlightingMode" },
			{ "PDFKit.PdfAnnotationKey", "PDFAnnotationKey" },
			{ "PDFKit.PdfAnnotationLineEndingStyle", "PDFAnnotationLineEndingStyle" },
			{ "PDFKit.PdfAnnotationSubtype", "PDFAnnotationSubtype" },
			{ "PDFKit.PdfAnnotationTextIconType", "PDFAnnotationTextIconType" },
			{ "PDFKit.PdfAnnotationWidgetSubtype", "PDFAnnotationWidgetSubtype" },
			{ "PDFKit.PdfAreaOfInterest", "PDFAreaOfInterest" },
			{ "PDFKit.PdfBorderStyle", "PDFBorderStyle" },
			{ "PDFKit.PdfDisplayBox", "PDFDisplayBox" },
			{ "PDFKit.PdfDisplayDirection", "PDFDisplayDirection" },
			{ "PDFKit.PdfDisplayMode", "PDFDisplayMode" },
			{ "PDFKit.PdfDocumentPermissions", "PDFDocumentPermissions" },
			{ "PDFKit.PdfInterpolationQuality", "PDFInterpolationQuality" },
			{ "PDFKit.PdfLineStyle", "PDFLineStyle" },
			{ "PDFKit.PdfMarkupType", "PDFMarkupType" },
			{ "PDFKit.PdfTextAnnotationIconType", "PDFTextAnnotationIconType" },
			{ "PDFKit.PdfThumbnailLayoutMode", "PDFThumbnailLayoutMode" },
			{ "PDFKit.PdfWidgetCellState", "PDFWidgetCellState" },
			{ "PDFKit.PdfWidgetControlType", "PDFWidgetControlType" },
			// Photos
			{ "Photos.PHAssetPlaybackStyle", "PHAsset.PlaybackStyle" },
			// ReplayKit
			{ "ReplayKit.RPRecordingError", "RPRecordingErrorCode" },
			// SafariServices
			{ "SafariServices.SFErrorCode", "SFError.Code" },
			// SceneKit
			{ "SceneKit.SCNAnimationImportPolicy", "SCNSceneSource.AnimationImportPolicy" },
			{ "SceneKit.SCNGeometrySourceSemantics", "SCNGeometrySource.Semantic" },
			{ "SceneKit.SCNPhysicsSearchMode", "SCNPhysicsWorld.TestSearchMode" },
			{ "SceneKit.SCNPhysicsShapeType", "SCNPhysicsShape.ShapeType" },
			{ "SceneKit.SCNRenderingApi", "SCNRenderingAPI" },
			// Security
			{ "Security.SecTrustResult", "SecTrustResultType" },
			{ "Security.SslAuthenticate", "SSLAuthenticate" },
			{ "Security.SslCipherSuite", "SSLCipherSuite" },
			{ "Security.SslCipherSuiteGroup", "SSLCiphersuiteGroup" },
			{ "Security.SslClientCertificateState", "SSLClientCertificateState" },
			{ "Security.SslConnectionType", "SSLConnectionType" },
			{ "Security.SslProtocol", "SSLProtocol" },
			{ "Security.SslProtocolSide", "SSLProtocolSide" },
			{ "Security.SslSessionOption", "SSLSessionOption" },
			{ "Security.SslSessionState", "SSLSessionState" },
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
			// WatchKit
			{ "WatchKit.WKErrorCode", "WatchKitError.Code" },
			// WebKit
			{ "WebKit.WKErrorCode", "WKError.Code" },
		};
	}
}
