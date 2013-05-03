using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using OpenTK.Platform;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using MonoTouch.AudioToolbox;

namespace MusicCube
{
	public class MusicCubePlayback : NSObject
	{
		private uint source;
		private uint buffer;
		private byte[] data;
		private float[] sourcePos = new float[3];
		private float[] listenerPos = new float[3];
		private float listenerRotation;

		#region Object Init / Maintenance

		public MusicCubePlayback ()
		{
			// initial position of the sound source and 
			// initial position and rotation of the listener
			// will be set by the view
			
			// setup our audio session
			AudioSession.Initialize ();
			AudioSession.Category = AudioSessionCategory.AmbientSound;
			AudioSession.SetActive (true);

			this.WasInterrupted = false;

			// Initialize our OpenAL environment
			this.InitOpenAL ();
		}

//		private void interruptionListener(	void *	inClientData,
//		                          UInt32	inInterruptionState)
//		{
//			MusicCubePlayback *THIS = (MusicCubePlayback*)inClientData;
//			if (inInterruptionState == kAudioSessionBeginInterruption)
//			{
//				// do nothing
//				[THIS teardownOpenAL];
//				if ([THIS isPlaying]) {
//					THIS->_wasInterrupted = YES;
//					THIS->_isPlaying = NO;
//				}
//			}
//			else if (inInterruptionState == kAudioSessionEndInterruption)
//			{
//				OSStatus result = AudioSessionSetActive(true);
//				if (result) printf("Error setting audio session active! %d\n", (int)result);
//				[THIS initOpenAL];
//				if (THIS->_wasInterrupted)
//				{
//					[THIS startSound];			
//					THIS->_wasInterrupted = NO;
//				}
//			}
//		}

		#endregion

		public bool IsPlaying { get; private set; }
		public bool WasInterrupted  { get; private set; }

		#region OpenAL

		private void InitBuffer ()
		{

			ALError error = ALError.NoError;
			ALFormat format;
			int size;
			double freq;

			NSBundle bundle = NSBundle.MainBundle;

			// get some audio data from a wave file
			using (var fileURL = NSUrl.FromFilename (bundle.PathForResource ("sound", "wav"))) {
				data = MyGetOpenALAudioData (fileURL, out size, out format, out freq);
			}

			if ((error = AL.GetError ()) != ALError.NoError) {
				throw new Exception (string.Format ("error loading sound: %x\n", error));
			}
				
			// use the static buffer data API
			AL.BufferData (Convert.ToInt32 (buffer), format, data, size, Convert.ToInt32 (freq));

			if ((error = AL.GetError ()) != ALError.NoError) {
				throw new Exception (string.Format ("error attaching audio to buffer: %x\n", error));
			}		
		}
		//		- (void) initBuffer
		//		{
		//			ALenum  error = AL_NO_ERROR;
		//			ALenum  format;
		//			ALsizei size;
		//			ALsizei freq;
		//			
		//			NSBundle*				bundle = [NSBundle mainBundle];
		//			
		//			// get some audio data from a wave file
		//			CFURLRef fileURL = (CFURLRef)[[NSURL fileURLWithPath:[bundle pathForResource:@"sound" ofType:@"wav"]] retain];
		//			
		//			if (fileURL)
		//			{	
		//				_data = MyGetOpenALAudioData(fileURL, &size, &format, &freq);
		//				CFRelease(fileURL);
		//				
		//				if((error = alGetError()) != AL_NO_ERROR) {
		//					printf("error loading sound: %x\n", error);
		//					exit(1);
		//				}
		//				
		//				// use the static buffer data API
		//				alBufferDataStaticProc(_buffer, format, _data, size, freq);
		//				
		//				if((error = alGetError()) != AL_NO_ERROR) {
		//					printf("error attaching audio to buffer: %x\n", error);
		//				}		
		//			}
		//			else
		//			{
		//				printf("Could not find file!\n");
		//				_data = NULL;
		//			}
		//		}
		//		

		public void InitSource ()
		{
			ALError error = ALError.NoError;
			AL.GetError (); // Clear the error
			
			// Turn Looping ON
			AL.Source (source, ALSourceb.Looping, true);

			// Set Source Position
			AL.Source (source, ALSource3f.Position, sourcePos [0], sourcePos [1], sourcePos [2]);

			// Set Source Reference Distance
			AL.Source (source, ALSourcef.ReferenceDistance, 0.15f);

			// attach OpenAL Buffer to OpenAL Source
			AL.Source (source, ALSourcei.Buffer, Convert.ToInt32 (buffer));

			if ((error = AL.GetError ()) != ALError.NoError) {
				throw new Exception ("Error attaching buffer to source: " + error.ToString ());
			}	
		}
		//		- (void) initSource
		//		{
		//			ALenum error = AL_NO_ERROR;
		//			alGetError(); // Clear the error
		//			
		//			// Turn Looping ON
		//			alSourcei(_source, AL_LOOPING, AL_TRUE);
		//			
		//			// Set Source Position
		//			alSourcefv(_source, AL_POSITION, _sourcePos);
		//			
		//			// Set Source Reference Distance
		//			alSourcef(_source, AL_REFERENCE_DISTANCE, 0.15f);
		//			
		//			// attach OpenAL Buffer to OpenAL Source
		//			alSourcei(_source, AL_BUFFER, _buffer);
		//			
		//			if((error = alGetError()) != AL_NO_ERROR) {
		//				printf("Error attaching buffer to source: %x\n", error);
		//				exit(1);
		//			}	
		//		}

		public void InitOpenAL ()
		{
			ALError error;
			OpenTK.ContextHandle newContext;
			IntPtr newDevice;

			// Create a new OpenAL Device
			// Pass NULL to specify the system’s default output device
			newDevice = Alc.OpenDevice (null);
			//if (newDevice != null) {
			// Create a new OpenAL Context
			// The new context will render to the OpenAL Device just created 
			newContext = Alc.CreateContext (newDevice, (int[])null);

			// Make the new context the Current OpenAL Context
			Alc.MakeContextCurrent (newContext);

			// Create some OpenAL Buffer Objects
			AL.GenBuffers (1, out buffer);
			if ((error = AL.GetError ()) != ALError.NoError) {
				throw new Exception (string.Format ("Error Generating Buffers: %x", error));
			}
					
			// Create some OpenAL Source Objects
			AL.GenSources (1, out source);
			if (AL.GetError () != ALError.NoError) {
				throw new Exception (string.Format ("Error generating sources! %x\n", error));
			}
					
			// clear any errors
			AL.GetError ();

			this.InitBuffer ();
			this.InitSource ();
		}
		//		- (void)initOpenAL
		//		{
		//			ALenum			error;
		//			ALCcontext		*newContext = NULL;
		//			ALCdevice		*newDevice = NULL;
		//			
		//			// Create a new OpenAL Device
		//			// Pass NULL to specify the system’s default output device
		//			newDevice = alcOpenDevice(NULL);
		//			if (newDevice != NULL)
		//			{
		//				// Create a new OpenAL Context
		//				// The new context will render to the OpenAL Device just created 
		//				newContext = alcCreateContext(newDevice, 0);
		//				if (newContext != NULL)
		//				{
		//					// Make the new context the Current OpenAL Context
		//					alcMakeContextCurrent(newContext);
		//					
		//					// Create some OpenAL Buffer Objects
		//					alGenBuffers(1, &_buffer);
		//					if((error = alGetError()) != AL_NO_ERROR) {
		//						printf("Error Generating Buffers: %x", error);
		//						exit(1);
		//					}
		//					
		//					// Create some OpenAL Source Objects
		//					alGenSources(1, &_source);
		//					if(alGetError() != AL_NO_ERROR) 
		//					{
		//						printf("Error generating sources! %x\n", error);
		//						exit(1);
		//					}
		//					
		//				}
		//			}
		//			// clear any errors
		//			alGetError();
		//			
		//			[self initBuffer];	
		//			[self initSource];
		//		}

		#endregion

		#region Play / Pause

		public void StartSound ()
		{
			ALError error;

			Console.WriteLine ("Start!\n");
			// Begin playing our source file
			AL.SourcePlay (source);
			if ((error = AL.GetError ()) != ALError.NoError) {
				Console.WriteLine ("error starting source: %x\n", error);
			} else {
				// Mark our state as playing
				this.IsPlaying = true;
			}
		}

		public void StopSound ()
		{
			ALError error;

			Console.WriteLine ("Stop!!\n");
			// Stop playing our source file
			AL.SourceStop (source);
			if ((error = AL.GetError ()) != ALError.NoError) {
				Console.WriteLine ("error stopping source: %x\n", error);
			} else {
				// Mark our state as not playing
				this.IsPlaying = false;
			}
		}

		#endregion

		#region Setters / Getters
		
		public float[] SourcePos {
			get { return sourcePos; }
			set {
				for (int i = 0; i < 3; i++) {
					sourcePos [i] = value [i];
				}
				
				// Move our audio source coordinates
				AL.Source (source, ALSource3f.Position, sourcePos [0], sourcePos [1], sourcePos [2]);
			}
		}
		
		public float[] ListenerPos {
			get { return listenerPos; }
			set { 
				for (int i=0; i<3; i++)
					listenerPos [i] = value [i];
				
				// Move our listener coordinates
				AL.Listener (ALListener3f.Position, listenerPos [0], listenerPos [1], listenerPos [2]);
			}
		}
		
		public float ListenerRotation {
			get { return listenerRotation; }
			set {
				var radians = value;
				listenerRotation = radians;
				float[] ori = {0, (float)Math.Cos (radians), (float)Math.Sin (radians), 1, 0, 0};
				
				// Set our listener orientation (rotation)
				AL.Listener (ALListenerfv.Orientation, ref ori);
			}
		}
		
		#endregion
		
		#region MyOpenALSupport.h

		private byte[] MyGetOpenALAudioData (NSUrl inFileURL, out int outDataSize, out ALFormat outDataFormat, out double outSampleRate)
		{
			UInt64 fileDataSize = 0;
			AudioStreamBasicDescription theFileFormat;
			byte[] theData = null;
			outDataSize = 0;
			outDataFormat = ALFormat.Mono8;
			outSampleRate = 0;
			
			// Open a file with ExtAudioFileOpen()
			using (var audioFile = AudioFile.Open(inFileURL,AudioFilePermission.Read)) {
				theFileFormat = audioFile.DataFormat.Value;

				if (theFileFormat.ChannelsPerFrame > 2) { 
					throw new Exception ("MyGetOpenALAudioData - Unsupported Format, channel count is greater than stereo\n"); 		
				}

				// TODO: convert TestAudioFormatNativeEndian method
//				if ((theFileFormat.Format != AudioFormatType.LinearPCM) || (!TestAudioFormatNativeEndian(theFileFormat))) { 
//					printf("MyGetOpenALAudioData - Unsupported Format, must be little-endian PCM\n"); goto Exit;
//				}
			
				if ((theFileFormat.BitsPerChannel != 8) && (theFileFormat.BitsPerChannel != 16)) { 
					throw new Exception ("MyGetOpenALAudioData - Unsupported Format, must be 8 or 16 bit PCM\n"); 
				}

				int throwAwaySize;
				// get the size of the data
				var fileDataSizeIntPtr = audioFile.GetProperty (AudioFileProperty.AudioDataByteCount, out throwAwaySize);
				fileDataSize = Convert.ToUInt64 (System.Runtime.InteropServices.Marshal.ReadInt64 (fileDataSizeIntPtr));
				outDataSize = Convert.ToInt32 (fileDataSize);
			
				theData = new byte[fileDataSize];

				// Read all the data into memory
				audioFile.Read (0, theData, 0, Convert.ToInt32 (fileDataSize), true);

				outDataFormat = (theFileFormat.ChannelsPerFrame > 1) ? ALFormat.Stereo16 : ALFormat.Mono16;
				outSampleRate = theFileFormat.SampleRate;

				return theData;
			}
		}
//		void* MyGetOpenALAudioData(CFURLRef inFileURL, ALsizei *outDataSize, ALenum *outDataFormat, ALsizei*	outSampleRate)
//		{
//			OSStatus						err = noErr;	
//			UInt64							fileDataSize = 0;
//			AudioStreamBasicDescription		theFileFormat;
//			UInt32							thePropertySize = sizeof(theFileFormat);
//			AudioFileID						afid = 0;
//			void*							theData = NULL;
//			
//			// Open a file with ExtAudioFileOpen()
//			err = AudioFileOpenURL(inFileURL, kAudioFileReadPermission, 0, &afid);
//			if(err) { printf("MyGetOpenALAudioData: AudioFileOpenURL FAILED, Error = %ld\n", err); goto Exit; }
//			
//			// Get the audio data format
//			err = AudioFileGetProperty(afid, kAudioFilePropertyDataFormat, &thePropertySize, &theFileFormat);
//			if(err) { printf("MyGetOpenALAudioData: AudioFileGetProperty(kAudioFileProperty_DataFormat) FAILED, Error = %ld\n", err); goto Exit; }
//			
//			if (theFileFormat.mChannelsPerFrame > 2)  { 
//				printf("MyGetOpenALAudioData - Unsupported Format, channel count is greater than stereo\n"); goto Exit;
//			}
//			
//			if ((theFileFormat.mFormatID != kAudioFormatLinearPCM) || (!TestAudioFormatNativeEndian(theFileFormat))) { 
//				printf("MyGetOpenALAudioData - Unsupported Format, must be little-endian PCM\n"); goto Exit;
//			}
//			
//			if ((theFileFormat.mBitsPerChannel != 8) && (theFileFormat.mBitsPerChannel != 16)) { 
//				printf("MyGetOpenALAudioData - Unsupported Format, must be 8 or 16 bit PCM\n"); goto Exit;
//			}
//			
//			
//			thePropertySize = sizeof(fileDataSize);
//			err = AudioFileGetProperty(afid, kAudioFilePropertyAudioDataByteCount, &thePropertySize, &fileDataSize);
//			if(err) { printf("MyGetOpenALAudioData: AudioFileGetProperty(kAudioFilePropertyAudioDataByteCount) FAILED, Error = %ld\n", err); goto Exit; }
//			
//			// Read all the data into memory
//			UInt32		dataSize = fileDataSize;
//			theData = malloc(dataSize);
//			if (theData)
//			{
//				AudioFileReadBytes(afid, false, 0, &dataSize, theData);
//				if(err == noErr)
//				{
//					// success
//					*outDataSize = (ALsizei)dataSize;
//					*outDataFormat = (theFileFormat.mChannelsPerFrame > 1) ? AL_FORMAT_STEREO16 : AL_FORMAT_MONO16;
//					*outSampleRate = (ALsizei)theFileFormat.mSampleRate;
//				}
//				else 
//				{ 
//					// failure
//					free (theData);
//					theData = NULL; // make sure to return NULL
//					printf("MyGetOpenALAudioData: ExtAudioFileRead FAILED, Error = %ld\n", err); goto Exit;
//				}	
//			}
//			
//		Exit:
//				// Dispose the ExtAudioFileRef, it is no longer needed
//				if (afid) AudioFileClose(afid);
//			return theData;
//		}

		public void TeardownOpenAL ()
		{
			// Delete the Sources
			AL.DeleteSources (1, ref source);
			// Delete the Buffers
			AL.DeleteBuffers (1, ref buffer);
			
			//Get active context (there can only be one)
			OpenTK.ContextHandle context = Alc.GetCurrentContext ();
			//Get device for active context
			IntPtr device = Alc.GetContextsDevice (context);
			//Release context
			Alc.DestroyContext (context);
			//Close device
			Alc.CloseDevice (device);
		}
//		void TeardownOpenAL()
//		{
//			ALCcontext	*context = NULL;
//			ALCdevice	*device = NULL;
//			ALuint		returnedName;
//			
//			// Delete the Sources
//			alDeleteSources(1, &returnedName);
//			// Delete the Buffers
//			alDeleteBuffers(1, &returnedName);
//			
//			//Get active context
//			context = alcGetCurrentContext();
//			//Get device for active context
//			device = alcGetContextsDevice(context);
//			//Release context
//			alcDestroyContext(context);
//			//Close device
//			alcCloseDevice(device);
//		}

		#endregion

	}
}

