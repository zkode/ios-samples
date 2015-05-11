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
using Foundation;
using System.Linq;

namespace MediaCapture
{
	public class Settings
	{
		enum SettingsNames
		{
			HaveSettingsBeenLoadedBefore,
			Camera,
			ImageCaptureEnabled,
			AudioCaptureEnabled,
			VideoCaptureEnabled,
			CaptureResolution,
			MaxMovieDurationInSeconds,
			AutoRecordNextMovie,
			SaveCapturedImagesToPhotoLibrary,
			SaveCapturedImagesToMyDocuments,
		}

		CameraType camera = CameraType.FrontFacing;
		public CameraType Camera {
			get {
				return camera;
			}
			set {
				camera = value;
			}
		}

		Resolution captureResolution = Resolution.Medium;
		public Resolution CaptureResolution {
			get {
				return captureResolution;
			}
			set {
				captureResolution = value;
			}
		}

		bool imageCaptureEnabled = true;
		public bool ImageCaptureEnabled {
			get {
				return imageCaptureEnabled;
			}
			set {
				imageCaptureEnabled = value;
			}
		}

		public bool AudioCaptureEnabled { get; set; }
		public bool VideoCaptureEnabled { get; set; }
		public bool SaveCapturedImagesToPhotoLibrary { get; set; }
		public bool SaveCapturedImagesToMyDocuments { get; set; }

		int imageSaveIntervalInSeconds = 5;
		public int ImageSaveIntervalInSeconds {
			get {
				return imageSaveIntervalInSeconds;
			}
		}

		// seconds (0 or negative number means no limit)
		int maxMovieDurationInSeconds = 60;
		public int MaxMovieDurationInSeconds {
			get {
				return maxMovieDurationInSeconds;
			}
			set {
				maxMovieDurationInSeconds = value;
			}
		}

		// whether or not to automatically start recording a new movie once the max duration is reached and recording is forcibly stopped
		bool autoRecordNextMovie = false;
		public bool AutoRecordNextMovie {
			get {
				return autoRecordNextMovie;
			}
			set {
				autoRecordNextMovie = value;
			}
		}

		static NSUrl DocumentsDir {
			get {
				return NSFileManager.DefaultManager
					.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)
					.Last();
			}
		}

		static string configDirectory = null;
		public static string ConfigDirectory {
			get {
				configDirectory = configDirectory ?? CreateDirectoryIfNeeded (Path.Combine (DocumentsDir.Path, "Config"));
				return configDirectory;
			}
		}

		static string videoDataPath = null;
		public static string VideoDataPath {
			get {
				videoDataPath = videoDataPath ?? CreateDirectoryIfNeeded (Path.Combine (DocumentsDir.Path, "VideoData"));
				return videoDataPath;
			}
		}

		static string imageDataPath = null;
		public static string ImageDataPath {
			get {
				imageDataPath = imageDataPath ?? CreateDirectoryIfNeeded (Path.Combine (DocumentsDir.Path, "ImageData"));
				return imageDataPath;
			}
		}

		public void Load()
		{
			bool isFirstSettingsLoad = (NSUserDefaults.StandardUserDefaults.BoolForKey( SettingsNames.HaveSettingsBeenLoadedBefore.ToString() ) == false);
			if ( isFirstSettingsLoad )
			{
				// this forces the defaults to be written and flag that this has happened for future loads
				NSUserDefaults.StandardUserDefaults.SetBool( true, SettingsNames.HaveSettingsBeenLoadedBefore.ToString() );
				Save ();
			}

			camera =  NSUserDefaults.StandardUserDefaults.IntForKey( SettingsNames.Camera.ToString()) == 0 ? CameraType.FrontFacing : CameraType.RearFacing ;
			ImageCaptureEnabled = NSUserDefaults.StandardUserDefaults.BoolForKey( SettingsNames.ImageCaptureEnabled.ToString() );
			AudioCaptureEnabled = NSUserDefaults.StandardUserDefaults.BoolForKey( SettingsNames.AudioCaptureEnabled.ToString() );
			VideoCaptureEnabled = NSUserDefaults.StandardUserDefaults.BoolForKey( SettingsNames.VideoCaptureEnabled.ToString() );
			//CaptureResolution = (Resolution) NSUserDefaults.StandardUserDefaults.IntForKey( SettingsNames.CaptureResolution.ToString() );
			CaptureResolution = Resolution.High;
			//MaxMovieDurationInSeconds = NSUserDefaults.StandardUserDefaults.IntForKey( SettingsNames.MaxMovieDurationInSeconds.ToString() );
			MaxMovieDurationInSeconds = 60;
			AutoRecordNextMovie = NSUserDefaults.StandardUserDefaults.BoolForKey( SettingsNames.AutoRecordNextMovie.ToString() );
			SaveCapturedImagesToPhotoLibrary = NSUserDefaults.StandardUserDefaults.BoolForKey( SettingsNames.SaveCapturedImagesToPhotoLibrary.ToString() );
			SaveCapturedImagesToMyDocuments = NSUserDefaults.StandardUserDefaults.BoolForKey( SettingsNames.SaveCapturedImagesToMyDocuments.ToString() );
		}

		public void Save()
		{
			NSUserDefaults.StandardUserDefaults.SetInt( (int)camera, SettingsNames.Camera.ToString() );
			NSUserDefaults.StandardUserDefaults.SetBool( ImageCaptureEnabled, SettingsNames.ImageCaptureEnabled.ToString() );
			NSUserDefaults.StandardUserDefaults.SetBool( AudioCaptureEnabled, SettingsNames.AudioCaptureEnabled.ToString() );
			NSUserDefaults.StandardUserDefaults.SetBool( VideoCaptureEnabled, SettingsNames.VideoCaptureEnabled.ToString() );
			NSUserDefaults.StandardUserDefaults.SetInt( (int)CaptureResolution, SettingsNames.CaptureResolution.ToString() );
			NSUserDefaults.StandardUserDefaults.SetBool( AutoRecordNextMovie, SettingsNames.AutoRecordNextMovie.ToString() );
			NSUserDefaults.StandardUserDefaults.SetInt( MaxMovieDurationInSeconds, SettingsNames.MaxMovieDurationInSeconds.ToString() );
			NSUserDefaults.StandardUserDefaults.SetBool( SaveCapturedImagesToPhotoLibrary, SettingsNames.SaveCapturedImagesToPhotoLibrary.ToString() );
			NSUserDefaults.StandardUserDefaults.SetBool( SaveCapturedImagesToMyDocuments, SettingsNames.SaveCapturedImagesToMyDocuments.ToString() );

			NSUserDefaults.StandardUserDefaults.Synchronize();
		}

		static string CreateDirectoryIfNeeded(string directory)
		{
			var fm = NSFileManager.DefaultManager;
			if (!fm.FileExists (directory)) {
				NSError error;
				if(!fm.CreateDirectory (directory, false, (NSFileAttributes)null, out error))
					Console.WriteLine (error);
			}

			return directory;
		}
	}
}