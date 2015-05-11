//
// how to capture still images, video and audio using iOS AVFoundation and the AVCAptureSession
//
// This sample handles all of the low-level AVFoundation and capture graph setup required to capture and save media.  This code also exposes the
// capture, configuration and notification capabilities in a more '.Netish' way of programming.  The client code will not need to deal with threads, delegate classes
// buffer management, or objective-C data types but instead will create .NET objects and handle standard .NET events.  The underlying iOS concepts and classes are detailed in
// the iOS developer online help (TP40010188-CH5-SW2).
//
// https://developer.apple.com/library/mac/#documentation/AudioVideo/Conceptual/AVFoundationPG/Articles/04_MediaCapture.html#//apple_ref/doc/uid/TP40010188-CH5-SW2
//
// Enhancements, suggestions and bug reports can be sent to steve.millar@infinitekdev.com
//
using System;
using System.IO;
using MonoTouch.Dialog;
using Foundation;
using UIKit;

namespace MediaCapture
{
	public class SettingsDialog
	{
		Settings settings = null;
		RootElement menu = null;

		// camera
		RadioElement fronCameraElement = null;
		RadioElement backCameraElement = null;
		RadioGroup cameraGroup = null;
		RootElement cameraElement = null;
		RadioElement lowResElement = null;
		RadioElement mediumResElement = null;
		RadioElement highResElement = null;
		RadioGroup resolutionGroup = null;
		RootElement resolutionElement = null;

		// still image capture
		BooleanElement imageCaptureEnabledElement = null;
		RadioElement dontSaveImagesElement = null;
		RadioElement saveImagesToPhotoLibraryElement = null;
		RadioElement saveImagesToMyDocumentsElement = null;
		RadioGroup saveImageGroup = null;
		RootElement saveImageElement = null;

		// media capture
		BooleanElement audioCaptureEnabledElement = null;
		BooleanElement videoCaptureEnabledElement = null;
		BooleanElement autoRecordNextMovieElement = null;
		RadioElement noLimitElement = null;
		RadioElement oneMinuteLimitElement = null;
		RadioElement fiveMinuteLimitElement = null;
		RadioElement tenMinuteLimitElement = null;
		RadioElement thirtyMinuteLimitElement = null;
		RadioGroup durationLimitGroup = null;
		RootElement durationElement = null;

		// actions
		StringElement deleteMoviesElement = null;
		StringElement deleteImagesElement = null;

		SettingsDialog()
		{
			
		}

		public SettingsDialog( Settings settings )
		{
			this.settings = settings;
		}

		public RootElement Menu {
			get {
				return BuildSettingsRootMenu();
			}
		}

		RootElement BuildSettingsRootMenu()
		{
			BuildCameraSettingsElements();
			BuildImageCaptureSettingsElements();
			BuildMediaCaptureSettingsElements();
			BuildActionElements();

			menu = new RootElement ("Settings")
			{
				new Section("Camera")
				{
					(Element)cameraElement,
					(Element)resolutionElement,
				},
				new Section("Still Images")
				{
					(Element)imageCaptureEnabledElement,
					(Element)saveImageElement,
				},
				new Section("Media")
				{
					audioCaptureEnabledElement,
					videoCaptureEnabledElement,
					autoRecordNextMovieElement,
					(Element)durationElement,
				},
				new Section("Actions")
				{
					deleteMoviesElement,
					deleteImagesElement,
				}
			};
			return menu;
		}

		void BuildCameraSettingsElements()
		{
			// camera
			fronCameraElement = new RadioElement("Front");
			backCameraElement = new RadioElement("Back");
			int index = (int)settings.Camera;
			cameraGroup = new RadioGroup("CameraGroup", index );
			cameraElement = new RootElement("Source Camera", cameraGroup)
			{
				new Section()
				{
					fronCameraElement,
					backCameraElement
				}
			};

			// resolution choices
			lowResElement = new RadioElement("Low");
			mediumResElement = new RadioElement("Medium");
			highResElement = new RadioElement("High");
			index = (int)settings.CaptureResolution;
			resolutionGroup = new RadioGroup("ResolutionGroup", index );
			resolutionElement = new RootElement("Resolution", resolutionGroup)
			{
				new Section()
				{
					lowResElement,
					mediumResElement,
					highResElement
				}
			};
		}

		void BuildImageCaptureSettingsElements()
		{
			imageCaptureEnabledElement = new BooleanElement("Capture", settings.ImageCaptureEnabled );
			imageCaptureEnabledElement.ValueChanged += delegate
			{
				EnforceDependencies();
			};

			dontSaveImagesElement = new RadioElement("Don't Save");
			saveImagesToPhotoLibraryElement = new RadioElement("Photo Library");
			saveImagesToMyDocumentsElement = new RadioElement("My Documents");

			int index = 0;
			if ( settings.SaveCapturedImagesToPhotoLibrary )
				index = 1;
			else if ( settings.SaveCapturedImagesToMyDocuments )
				index = 2;
			saveImageGroup = new RadioGroup("SaveImagesGroup", index );
			saveImageElement = new RootElement("Save To", saveImageGroup )
			{
				new Section()
				{
					dontSaveImagesElement,
					saveImagesToPhotoLibraryElement,
					saveImagesToMyDocumentsElement
				}
			};
		}

		void BuildMediaCaptureSettingsElements()
		{
			audioCaptureEnabledElement = new BooleanElement("Record Audio", settings.AudioCaptureEnabled );
			audioCaptureEnabledElement.ValueChanged += delegate
			{
				EnforceDependencies();
			};
			videoCaptureEnabledElement = new BooleanElement("Record Video", settings.VideoCaptureEnabled );
			videoCaptureEnabledElement.ValueChanged += delegate
			{
				EnforceDependencies();
			};
			autoRecordNextMovieElement = new BooleanElement("Loop Recordings", settings.AutoRecordNextMovie );

			// duration choices
			noLimitElement = new RadioElement("Unlimited");
			oneMinuteLimitElement = new RadioElement("1 Minute");
			fiveMinuteLimitElement = new RadioElement("5 Minutes");
			tenMinuteLimitElement = new RadioElement("10 Minutes");
			thirtyMinuteLimitElement = new RadioElement("30 Minutes");
			int index = 0;
			int duration = settings.MaxMovieDurationInSeconds;
			if ( duration <= 0 )         { index = 0; }
			else if ( duration <= 60 )   { index = 1; }
			else if ( duration <= 300 )  { index = 2; }
			else if ( duration <= 600 )  { index = 3; }
			else if ( duration <= 1800 ) { index = 4; }

			durationLimitGroup = new RadioGroup("DurationGroup", index );
			durationElement = new RootElement("Maximum Time", durationLimitGroup )
			{
				new Section()
				{
					noLimitElement,
					oneMinuteLimitElement,
					fiveMinuteLimitElement,
					tenMinuteLimitElement,
					thirtyMinuteLimitElement
				}
			};
		}

		void BuildActionElements()
		{
			deleteMoviesElement = new StringElement("Delete Movies");
			deleteMoviesElement.Tapped += () => {
				Directory.Delete( Settings.VideoDataPath, true );
			};

			deleteImagesElement = new StringElement("Delete Images");
			deleteImagesElement.Tapped += () => {
				Directory.Delete( Settings.ImageDataPath, true );
			};
		}

		public void EnforceDependencies()
		{
			// image capture save is not relevant if no images are being captured
			if ( imageCaptureEnabledElement.Value == false )
				saveImageGroup.Selected = 0;

			UITableViewCell activeCell = saveImageElement.GetActiveCell ();
			if(activeCell == null)
				return;

			activeCell.UserInteractionEnabled = imageCaptureEnabledElement.Value;

			// looped recordings and duration are not relevant unless something is being recorded
			bool isMediaCaptureEnebled = ( audioCaptureEnabledElement.Value || videoCaptureEnabledElement.Value );
			if ( isMediaCaptureEnebled == false )
				autoRecordNextMovieElement.Value = false;
			autoRecordNextMovieElement.GetActiveCell().UserInteractionEnabled = isMediaCaptureEnebled;
			durationElement.GetActiveCell().UserInteractionEnabled = isMediaCaptureEnebled;
		}

		public Settings ResultSettings {
			get {
				Settings retVal = new Settings();

				// camera
				retVal.Camera = (CameraType)cameraGroup.Selected;
				retVal.CaptureResolution = (Resolution)resolutionGroup.Selected;

				// image capture
				retVal.ImageCaptureEnabled = imageCaptureEnabledElement.Value;
				retVal.SaveCapturedImagesToPhotoLibrary = (saveImageGroup.Selected == 1);
				retVal.SaveCapturedImagesToMyDocuments = (saveImageGroup.Selected == 2);

				// media capture
				retVal.AudioCaptureEnabled = audioCaptureEnabledElement.Value;
				retVal.VideoCaptureEnabled = videoCaptureEnabledElement.Value;
				retVal.AutoRecordNextMovie = autoRecordNextMovieElement.Value;

				// duration of recording
				int numSeconds = 0;
				int index = durationLimitGroup.Selected;
				if ( index == 0 )         { numSeconds = 0; }
				else if ( index == 1 )    { numSeconds = 60; }
				else if ( index == 2 )    { numSeconds = 300; }
				else if ( index == 3 )    { numSeconds = 600; }
				else if ( index == 4 )    { numSeconds = 1800; }
				retVal.MaxMovieDurationInSeconds = numSeconds;

				return retVal;
			}
		}
	}
}