using System;

using OpenTK;
using GL1 = OpenTK.Graphics.ES11.GL;
using All1 = OpenTK.Graphics.ES11.All;
using OpenTK.Platform.iPhoneOS;

using MonoTouch.Foundation;
using MonoTouch.CoreAnimation;
using MonoTouch.ObjCRuntime;
using MonoTouch.OpenGLES;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using System.Runtime.InteropServices;

namespace MusicCube
{
	[Register ("EAGLView")]
	public class EAGLView : iPhoneOSGameView
	{
		
		private bool started = false;
		
		[Export ("initWithCoder:")]
		public EAGLView (NSCoder coder) : base (coder)
		{
			LayerRetainsBacking = false;
			LayerColorFormat = EAGLColorFormat.RGBA8;
			ContextRenderingApi = EAGLRenderingAPI.OpenGLES1;
		}
		
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			
			mode = 1;
			
			// create vertex arrays for the circle paths
			this.BuildCircleVertices (ref innerCircleVertices, kCircleSegments, kInnerCircleRadius);
			this.BuildCircleVertices (ref outerCircleVertices, kCircleSegments, kOuterCircleRadius);
			
			// load textures
			string[] textureName = { @"speaker.png", @"nums.png", @"info.png", @"instr.png", @"description.png" };		
			
			GL1.GenTextures (5, texture);
			
			int i;
			for (i=0; i<5; i++) {
				GL1.BindTexture (All1.Texture2D, texture [i]);
				this.LoadTexture (textureName [i]);
			}
			
			GL1.BindTexture (All1.Texture2D, 0);
			
			// pulled from original layoutSubViews method
			this.SetTeapotMaterial ();
			
			if (!showDesc)
				playback.StartSound ();
		}
		
		[Export ("layerClass")]
		public static new Class GetLayerClass ()
		{
			return iPhoneOSGameView.GetLayerClass ();
		}
		
		protected override void ConfigureLayer (CAEAGLLayer eaglLayer)
		{
			eaglLayer.Opaque = true;
		}
		
		public bool IsAnimating { get; private set; }
		
		public void StartAnimating ()
		{
			if (IsAnimating)
				return;
			
			if (started)
				this.Stop ();
			
			started = true;
			
			IsAnimating = true;
			
			this.Run (60.0);
		}
		
		public void StopAnimating ()
		{
			if (started)
				this.Stop ();
			
			this.Run (5.0);
			
			IsAnimating = false;
		}
		
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			base.OnRenderFrame (e);
			
			MakeCurrent ();
			
			GL1.Viewport (0, 0, Size.Width, Size.Height);
			
			this.DrawView ();
			
			SwapBuffers ();
		}
		
		#region Member Definitions - EAGLView.h
		
		private const int kCircleSegments = 36;
		
		private uint mode;
		
		private float[] innerCircleVertices = new float[kCircleSegments * 3], outerCircleVertices = new float[kCircleSegments * 3];
		
		// teapot
		private float rot;
		
		// cube
		private float[] cubePos = new float[3];
		private float cubeRot;
		
		private uint[] texture = new uint[5];
		
		private bool showDesc;
		
		// OpenAL playback is wired up in IB
		private MusicCubePlayback playback = new MusicCubePlayback ();
		
		#endregion
		
		#region Member Definitions - EAGLView.m
		
		//private const int USE_DEPTH_BUFFER = 1;
		
		private const float kInnerCircleRadius = 1.0f;
		private const float kOuterCircleRadius = 1.1f;
		
		private const float kTeapotScale = 1.8f;
		private const float kCubeScale = 0.12f;
		private const float kButtonScale = 0.1f;
		
		private const float kButtonLeftSpace = 1.1f;
		#endregion
		
		#region Init
		
		private void BuildCircleVertices (ref float[] vertices, int segments, float radius)
		{
			float segmentDegrees = 360.0f / segments;
			
			int count = 0;
			for (float i = 0; i < 360.0f; i += segmentDegrees) {
				vertices [count++] = 0;									//x
				vertices [count++] = (float)Math.Cos (DegreesToRadians (i)) * radius;	//y
				vertices [count++] = (float)Math.Sin (DegreesToRadians (i)) * radius;	//z
			}
		}
		//		- (void)buildCircleVertices:(GLfloat*)vertices withNumOfSegments:(GLint)segments radius:(GLfloat)radius
		//		{
		//			GLint count=0;
		//			for (GLfloat i = 0; i < 360.0f; i += 360.0f/segments)
		//			{
		//				vertices[count++] = 0;									//x
		//				vertices[count++] = (cos(DegreesToRadians(i))*radius);	//y
		//				vertices[count++] = (sin(DegreesToRadians(i))*radius);	//z
		//			}
		//		}
		
		private float DegreesToRadians (float degrees)
		{
			return degrees * (float)Math.PI / 180.0f;
		}
		
		private float RadiansToDegrees (float radians)
		{
			return radians * 180.0f / (float)Math.PI;
		}
		
		private void SetTeapotMaterial ()
		{
			float[] lightAmbient = {0.2f, 0.2f, 0.2f, 1.0f};
			float[] lightDiffuse = {0.2f, 0.7f, 0.2f, 1.0f};
			float[] matAmbient = {0.4f, 0.8f, 0.4f, 1.0f};
			float[] matDiffuse = {1.0f, 1.0f, 1.0f, 1.0f};
			float[] matSpecular = {1.0f, 1.0f, 1.0f, 1.0f};
			float[] lightPosition = {0.0f, 0.0f, 1.0f, 0.0f};
			float lightShininess = 100.0f;
			
			GL1.Material (All1.FrontAndBack, All1.Ambient, matAmbient);
			GL1.Material (All1.FrontAndBack, All1.Diffuse, matDiffuse);
			GL1.Material (All1.FrontAndBack, All1.Specular, matSpecular);
			GL1.Material (All1.FrontAndBack, All1.Shininess, lightShininess);
			GL1.Light (All1.Light0, All1.Ambient, lightAmbient);
			GL1.Light (All1.Light0, All1.Diffuse, lightDiffuse);
			GL1.Light (All1.Light0, All1.Position, lightPosition);
			GL1.ShadeModel (All1.Smooth);
		}
		//		- (void) setTeapotMaterial
		//		{
		//			const GLfloat			lightAmbient[] = {0.2, 0.2, 0.2, 1.0};
		//			const GLfloat			lightDiffuse[] = {0.2, 0.7, 0.2, 1.0};
		//			const GLfloat			matAmbient[] = {0.4, 0.8, 0.4, 1.0};
		//			const GLfloat			matDiffuse[] = {1.0, 1.0, 1.0, 1.0};	
		//			const GLfloat			matSpecular[] = {1.0, 1.0, 1.0, 1.0};
		//			const GLfloat			lightPosition[] = {0.0, 0.0, 1.0, 0.0}; 
		//			const GLfloat			lightShininess = 100.0;
		//			
		//			glMaterialfv(GL_FRONT_AND_BACK, GL_AMBIENT, matAmbient);
		//			glMaterialfv(GL_FRONT_AND_BACK, GL_DIFFUSE, matDiffuse);
		//			glMaterialfv(GL_FRONT_AND_BACK, GL_SPECULAR, matSpecular);
		//			glMaterialf(GL_FRONT_AND_BACK, GL_SHININESS, lightShininess);
		//			glLightfv(GL_LIGHT0, GL_AMBIENT, lightAmbient);
		//			glLightfv(GL_LIGHT0, GL_DIFFUSE, lightDiffuse);
		//			glLightfv(GL_LIGHT0, GL_POSITION, lightPosition); 			
		//			glShadeModel(GL_SMOOTH);
		//		}
		
		private void LoadTexture (string name)
		{
			using (UIImage image = UIImage.FromBundle (name)) {
				CGContext texContext;
				byte[] bytes = null;
				int width, height;
				
				if (image != null) {
					width = image.CGImage.Width;
					height = image.CGImage.Height;
					
					bytes = new byte[width * height * 4];
					// Uses the bitmap creation function provided by the Core Graphics framework. 
					using (texContext = new CGBitmapContext (bytes, width, height, 8, width * 4, image.CGImage.ColorSpace, CGImageAlphaInfo.PremultipliedLast)) {
						// After you create the context, you can draw the image to the context.
						texContext.DrawImage (new System.Drawing.RectangleF (0.0f, 0.0f, (float)width, (float)height), image.CGImage);
						
						// setup texture parameters
						GL1.TexParameter (All1.Texture2D, All1.TextureMagFilter, (int)All1.Linear);
						GL1.TexParameter (All1.Texture2D, All1.TextureMinFilter, (int)All1.Linear);
						GL1.TexParameter (All1.Texture2D, All1.TextureWrapS, (int)All1.ClampToEdge);
						GL1.TexParameter (All1.Texture2D, All1.TextureWrapT, (int)All1.ClampToEdge);
						
						GL1.TexImage2D (All1.Texture2D, 0, (int)All1.Rgba, width, height, 0, All1.Rgba, All1.UnsignedByte, bytes);
					}
				}
			}
		}
		//		- (void)loadTexture:(NSString *)name
		//		{
		//			CGImageRef image = [UIImage imageNamed:name].CGImage;
		//			CGContextRef texContext;
		//			GLubyte* bytes = nil;	
		//			size_t	width, height;
		//			
		//			if (image) {
		//				width = CGImageGetWidth(image);
		//				height = CGImageGetHeight(image);
		//				
		//				bytes = (GLubyte*) calloc(width*height*4, sizeof(GLubyte));
		//				// Uses the bitmatp creation function provided by the Core Graphics framework. 
		//				texContext = CGBitmapContextCreate(bytes, width, height, 8, width * 4, CGImageGetColorSpace(image), kCGImageAlphaPremultipliedLast);
		//				// After you create the context, you can draw the image to the context.
		//				CGContextDrawImage(texContext, CGRectMake(0.0, 0.0, (CGFloat)width, (CGFloat)height), image);
		//				// You don't need the context at this point, so you need to release it to avoid memory leaks.
		//				CGContextRelease(texContext);
		//				
		//				// setup texture parameters
		//				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		//				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		//				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		//				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		//				
		//				glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, bytes);
		//				free(bytes);
		//			}
		//		}
		
		#endregion
		
		#region Draw
		
		private void DrawCircle (float[] vertices, int segments)
		{
			GL1.EnableClientState (All1.VertexArray);
			
			GL1.VertexPointer (3, All1.Float, 0, vertices);
			
			GL1.PushMatrix ();
			GL1.Color4 (0.2f, 0.7f, 0.2f, 1.0f);
			GL1.DrawArrays (All1.LineLoop, 0, segments);
			GL1.PopMatrix ();
			
			GL1.DisableClientState (All1.VertexArray);
		}
		//		- (void)drawCircle:(GLfloat*)vertices withNumOfSegments:(GLint)segments
		//		{
		//			glEnableClientState(GL_VERTEX_ARRAY);
		//			
		//			glVertexPointer (3, GL_FLOAT, 0, vertices); 
		//			
		//			glPushMatrix();
		//			glColor4f(0.2f, 0.7f, 0.2f, 1.0f);
		//			glDrawArrays (GL_LINE_LOOP, 0, segments);
		//			glPopMatrix();
		//			
		//			glDisableClientState(GL_VERTEX_ARRAY);
		//		}
		//		
		
		private void DrawTeapot ()
		{
			int start = 0, i = 0;
			
			GL1.Enable (All1.Lighting);
			GL1.Enable (All1.Light0);
			GL1.Enable (All1.Normalize);
			GL1.EnableClientState (All1.VertexArray);
			GL1.EnableClientState (All1.NormalArray);
			
			GL1.VertexPointer (3, All1.Float, 0, Teapot.teapot_vertices);
			GL1.NormalPointer (All1.Float, 0, Teapot.teapot_normals);
			
			if (!showDesc)
				rot -= 1.0f;
			float radius = (kOuterCircleRadius + kInnerCircleRadius) / 2.0f;
			float[] teapotPos = {
				0.0f,
				(float)Math.Cos (DegreesToRadians (rot)) * radius,
				(float)Math.Sin (DegreesToRadians (rot)) * radius
			};
			
			GL1.PushMatrix ();
			GL1.LoadIdentity ();
			
			// move clockwise along the circle
			GL1.Translate (teapotPos [0], teapotPos [1], teapotPos [2]);
			GL1.Scale (kTeapotScale, kTeapotScale, kTeapotScale);
			
			// add rotation;
			float rotYInRadians;
			if (mode == 2 || mode == 4)
				// in mode 2 and 4, the teapot (listener) always faces to one direction
				rotYInRadians = 0.0f;
			else
				// in mode 1 and 3, the teapot (listener) always faces to the cube (sound source)
				rotYInRadians = (float)Math.Atan2 (Convert.ToDouble (teapotPos [2] - cubePos [2]), Convert.ToDouble (teapotPos [1] - cubePos [1]));
			
			GL1.Rotate (-90.0f, 0, 0, 1); //we want to display in landscape mode
			GL1.Rotate (RadiansToDegrees (rotYInRadians), 0, 1, 0);
			
			// draw the teapot
			while (i < Teapot.num_teapot_indices) {
				if (Teapot.teapot_indices [i] == -1) {
					this.DrawTeapotPatch (start, i - start);
					start = i + 1;
				}
				i++;
			}
			if (start < Teapot.num_teapot_indices) {
				this.DrawTeapotPatch (start, i - start - 1);
			}
			
			GL1.PopMatrix ();
			
			GL1.Disable (All1.Lighting);
			GL1.Disable (All1.Light0);
			GL1.Disable (All1.Normalize);
			GL1.DisableClientState (All1.VertexArray);
			GL1.DisableClientState (All1.NormalArray);
			
			// update playback
			playback.ListenerPos = teapotPos; //listener's position
			playback.ListenerRotation = rotYInRadians - (float)Math.PI; //listener's rotation in Radians
		}
		
		private void DrawTeapotPatch (int start, int patchIndicesCount)
		{
			var curPatchIndices = new short[patchIndicesCount];
			Array.Copy (Teapot.teapot_indices, start, curPatchIndices, 0, patchIndicesCount);
			GL1.DrawElements (All1.TriangleStrip, patchIndicesCount, All1.UnsignedShort, curPatchIndices);
			// TODO: look into the usage of GL1.DrawElements(..) using the (.., ref T3 indices) overload
		}
		
		//		- (void)drawTeapot
		//		{
		//			int	start = 0, i = 0;
		//			
		//			glEnable(GL_LIGHTING);
		//			glEnable(GL_LIGHT0);
		//			glEnable(GL_NORMALIZE);
		//			glEnableClientState(GL_VERTEX_ARRAY);
		//			glEnableClientState(GL_NORMAL_ARRAY);
		//			
		//			glVertexPointer(3 ,GL_FLOAT, 0, teapot_vertices);
		//			glNormalPointer(GL_FLOAT, 0, teapot_normals);
		//			
		//			if (!showDesc) rot -= 1.0f;
		//			GLfloat radius = (kOuterCircleRadius + kInnerCircleRadius) / 2.;
		//			GLfloat teapotPos[3] = {0.0f, cos(DegreesToRadians(rot))*radius, sin(DegreesToRadians(rot))*radius};
		//			
		//			glPushMatrix();
		//			glLoadIdentity();
		//			
		//			// move clockwise along the circle
		//			glTranslatef(teapotPos[0], teapotPos[1], teapotPos[2]);
		//			glScalef(kTeapotScale, kTeapotScale, kTeapotScale);
		//			
		//			// add rotation;
		//			GLfloat rotYInRadians;
		//			if (mode == 2 || mode == 4)
		//				// in mode 2 and 4, the teapot (listener) always faces to one direction
		//				rotYInRadians = 0.0f;
		//			else
		//				// in mode 1 and 3, the teapot (listener) always faces to the cube (sound source)
		//				rotYInRadians = atan2(teapotPos[2]-cubePos[2], teapotPos[1]-cubePos[1]);
		//			
		//			glRotatef(-90.0f, 0, 0, 1); //we want to display in landscape mode
		//			glRotatef(RadiansToDegrees(rotYInRadians), 0, 1, 0);
		//			
		//			// draw the teapot
		//			while(i < num_teapot_indices) {
		//				if(teapot_indices[i] == -1) {
		//					glDrawElements(GL_TRIANGLE_STRIP, i - start, GL_UNSIGNED_SHORT, &teapot_indices[start]);
		//					start = i + 1;
		//				}
		//				i++;
		//			}
		//			if(start < num_teapot_indices)
		//				glDrawElements(GL_TRIANGLE_STRIP, i - start - 1, GL_UNSIGNED_SHORT, &teapot_indices[start]);
		//			
		//			glPopMatrix();
		//			
		//			glDisable(GL_LIGHTING);
		//			glDisable(GL_LIGHT0);
		//			glDisable(GL_NORMALIZE);
		//			glDisableClientState(GL_VERTEX_ARRAY);
		//			glDisableClientState(GL_NORMAL_ARRAY);
		//			
		//			// update playback
		//			playback.listenerPos = teapotPos; //listener's position
		//			playback.listenerRotation = rotYInRadians - M_PI; //listener's rotation in Radians
		//		}
		//		
		
		// simple cube data
		// our sound source is omnidirectional, adjust the vertices 
		// so that speakers in textures point to all different directions
		private readonly float[][] cubeVertices = {
			new float[] { 1,-1, 1, -1,-1, 1,  1, 1, 1, -1, 1, 1 },
			new float[] { 1, 1, 1,  1,-1, 1,  1, 1,-1,  1,-1,-1 },
			new float[] {-1, 1,-1, -1,-1,-1, -1, 1, 1, -1,-1, 1 },
			new float[] { 1, 1, 1, -1, 1, 1,  1, 1,-1, -1, 1,-1 },
			new float[] { 1,-1,-1, -1,-1,-1,  1, 1,-1, -1, 1,-1 },
			new float[] { 1,-1, 1, -1,-1, 1,  1,-1,-1, -1,-1,-1 },
		};
		
		private readonly float[][] cubeColors = {
			new float[] {1, 0, 0, 1}, new float[] {0, 1, 0, 1}, new float[] {0, 0, 1, 1}, new float[] {1, 1, 0, 1}, new float[] {
				0,
				1,
				1,
				1
			}, new float[] {
				1,
				0,
				1,
				1
			},
		};
		
		private readonly float[] cubeTexCoords = new float[8] {
			1, 0,  1, 1,  0, 0,  0, 1,
		};
		
		private void DrawCube ()
		{	
			GL1.BindTexture (All1.Texture2D, texture [0]);
			
			if (!showDesc)
				cubeRot += 3;
			
			GL1.PushMatrix ();
			GL1.LoadIdentity ();
			GL1.Translate (cubePos [0], cubePos [1], cubePos [2]); 
			GL1.Scale (kCubeScale, kCubeScale, kCubeScale);
			
			if (mode <= 2)
				// origin of the teapot is at its bottom, but 
				// origin of the cube is at its center, so move up a unit to put the cube on surface
				// we'll pass the bottom of the cube (cubePos) to the playback
				GL1.Translate (1.0f, 0.0f, 0.0f);
			else
				// in mode 3 and 4, simply move up the cube a bit more to avoid colliding with the teapot
				GL1.Translate (4.5f, 0.0f, 0.0f);
			
			// rotate around to simulate the omnidirectional effect
			GL1.Rotate (cubeRot, 1, 0, 0);
			GL1.Rotate (cubeRot, 0, 1, 1);
			
			GL1.TexCoordPointer (2, All1.Float, 0, cubeTexCoords);
			
			int f;
			for (f = 0; f < 6; f++) {
				GL1.Color4 (cubeColors [f] [0], cubeColors [f] [1], cubeColors [f] [2], cubeColors [f] [3]);
				GL1.VertexPointer (3, All1.Float, 0, cubeVertices [f]);
				GL1.DrawArrays (All1.TriangleStrip, 0, 4);
			}
			
			GL1.PopMatrix ();
			
			GL1.BindTexture (All1.Texture2D, 0);
		}
		//		-(void)drawCube
		//		{	
		//			// simple cube data
		//			// our sound source is omnidirectional, adjust the vertices 
		//			// so that speakers in textures point to all different directions
		//			const GLfloat cubeVertices[6][12] = {
		//				{ 1,-1, 1, -1,-1, 1,  1, 1, 1, -1, 1, 1 },
		//				{ 1, 1, 1,  1,-1, 1,  1, 1,-1,  1,-1,-1 },
		//				{-1, 1,-1, -1,-1,-1, -1, 1, 1, -1,-1, 1 },
		//				{ 1, 1, 1, -1, 1, 1,  1, 1,-1, -1, 1,-1 },
		//				{ 1,-1,-1, -1,-1,-1,  1, 1,-1, -1, 1,-1 },
		//				{ 1,-1, 1, -1,-1, 1,  1,-1,-1, -1,-1,-1 },
		//			};
		//			
		//			const GLfloat cubeColors[6][4] = {
		//				{1, 0, 0, 1}, {0, 1, 0, 1}, {0, 0, 1, 1}, {1, 1, 0, 1}, {0, 1, 1, 1}, {1, 0, 1, 1},
		//			};
		//			
		//			const GLfloat cubeTexCoords[8] = {
		//				1, 0,  1, 1,  0, 0,  0, 1,
		//			};
		//			
		//			glBindTexture(GL_TEXTURE_2D, texture[0]);
		//			
		//			if (!showDesc) cubeRot += 3;
		//			
		//			glPushMatrix();
		//			glLoadIdentity();
		//			glTranslatef(cubePos[0], cubePos[1], cubePos[2]); 
		//			glScalef(kCubeScale, kCubeScale, kCubeScale);
		//			
		//			if (mode <= 2)
		//				// origin of the teapot is at its bottom, but 
		//				// origin of the cube is at its center, so move up a unit to put the cube on surface
		//				// we'll pass the bottom of the cube (cubePos) to the playback
		//				glTranslatef(1.0f, 0.0f, 0.0f);
		//			else
		//				// in mode 3 and 4, simply move up the cube a bit more to avoid colliding with the teapot
		//				glTranslatef(4.5f, 0.0f, 0.0f);
		//			
		//			// rotate around to simulate the omnidirectional effect
		//			glRotatef(cubeRot, 1, 0, 0);
		//			glRotatef(cubeRot, 0, 1, 1);
		//			
		//			glTexCoordPointer(2, GL_FLOAT, 0, cubeTexCoords);
		//			int f;
		//			for (f = 0; f < 6; f++) {
		//				glColor4f(cubeColors[f][0], cubeColors[f][1], cubeColors[f][2], cubeColors[f][3]);
		//				glVertexPointer(3, GL_FLOAT, 0, cubeVertices[f]);
		//				glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
		//			}
		//			
		//			glPopMatrix();
		//			
		//			glBindTexture(GL_TEXTURE_2D, 0);
		//		}
		//		
		
		private readonly float[] buttonVertices = {
			-1,-1,0,  1,-1,0, -1, 1,0,  1, 1,0
		};
		
		// numbers 1-4 are stored in a sprite sheet
		// in the first row, numbers are shown as unselected
		private readonly float[][] buttonNotSelectedTexCoords = {
			new float[] {0.25f, 0.5f,   0.25f, 0,   0,    0.5f,   0,    0}, //1
			new float[] {0.5f,  0.5f,   0.5f,  0,   0.25f, 0.5f,   0.25f, 0}, //2
			new float[] {0.75f, 0.5f,   0.75f, 0,   0.5f,  0.5f,   0.5f,  0}, //3
			new float[] {1,    0.5f,   1,    0,   0.75f, 0.5f,   0.75f, 0} //4
		};
		
		// in the second row, numbers are shown as selected
		private readonly float[][] buttonSelectedTexCoords = {
			new float[] {0.25f, 1,   0.25f, 0.5f,   0,    1,   0,    0.5f}, //1
			new float[] {0.5f,  1,   0.5f,  0.5f,   0.25f, 1,   0.25f, 0.5f}, //2
			new float[] {0.75f, 1,   0.75f, 0.5f,   0.5f,  1,   0.5f,  0.5f}, //3
			new float[] {1,    1,   1,    0.5f,   0.75f, 1,   0.75f, 0.5f} //4
		};
		
		private void DrawModes ()
		{
			GL1.BindTexture (All1.Texture2D, texture [1]);
			
			GL1.VertexPointer (3, All1.Float, 0, buttonVertices);
			
			// draw each button in its right mode (selected/unselected)
			int i;
			for (i=0; i<4; i++) {
				GL1.PushMatrix ();
				GL1.LoadIdentity ();
				GL1.Translate (-1.0f, 1.5f - kButtonLeftSpace, 0.0f); //move to the bottom-left corner (in landscape)
				GL1.Scale (kButtonScale, kButtonScale, 1.0f);
				GL1.Translate (1.0f, -1.0f - 2 * i, 0.0f); //move to the current grid
				GL1.Color4 (1.0f, 1.0f, 1.0f, 1.0f);
				if (mode == i + 1) //is currently selected
					GL1.TexCoordPointer (2, All1.Float, 0, buttonSelectedTexCoords [i]);
				else
					GL1.TexCoordPointer (2, All1.Float, 0, buttonNotSelectedTexCoords [i]);
				GL1.DrawArrays (All1.TriangleStrip, 0, 4);
				GL1.PopMatrix ();
			}
			
			GL1.BindTexture (All1.Texture2D, 0);
		}
		//		- (void)drawModes
		//		{
		//			glBindTexture(GL_TEXTURE_2D, texture[1]);
		//			
		//			const GLfloat buttonVertices[] = {
		//				-1,-1,0,  1,-1,0, -1, 1,0,  1, 1,0
		//			};
		//			
		//			// numbers 1-4 are stored in a sprite sheet
		//			// in the first row, numbers are shown as unselected
		//			const GLfloat buttonNotSelectedTexCoords[4][8] = {
		//				{0.25, 0.5,   0.25, 0,   0,    0.5,   0,    0}, //1
		//				{0.5,  0.5,   0.5,  0,   0.25, 0.5,   0.25, 0}, //2
		//				{0.75, 0.5,   0.75, 0,   0.5,  0.5,   0.5,  0}, //3
		//				{1,    0.5,   1,    0,   0.75, 0.5,   0.75, 0}, //4
		//			};
		//			
		//			// in the second row, numbers are shown as seleted
		//			const GLfloat buttonSelectedTexCoords[4][8] = {
		//				{0.25, 1,   0.25, 0.5,   0,    1,   0,    0.5}, //1
		//				{0.5,  1,   0.5,  0.5,   0.25, 1,   0.25, 0.5}, //2
		//				{0.75, 1,   0.75, 0.5,   0.5,  1,   0.5,  0.5}, //3
		//				{1,    1,   1,    0.5,   0.75, 1,   0.75, 0.5}, //4
		//			};
		//			
		//			glVertexPointer(3, GL_FLOAT, 0, buttonVertices);
		//			
		//			// draw each button in its right mode (selected/unselected)
		//			int i;
		//			for (i=0; i<4; i++) 
		//			{
		//				glPushMatrix();
		//				glLoadIdentity();
		//				glTranslatef(-1.0f, 1.5f-kButtonLeftSpace, 0.0f); //move to the bottom-left corner (in landscape)
		//				glScalef(kButtonScale, kButtonScale, 1.0f);
		//				glTranslatef(1.0f, -1.0f-2*i, 0.0f); //move to the current grid
		//				glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
		//				if (mode == i+1) //is currently selected
		//					glTexCoordPointer(2, GL_FLOAT, 0, buttonSelectedTexCoords[i]);
		//				else
		//					glTexCoordPointer(2, GL_FLOAT, 0, buttonNotSelectedTexCoords[i]);
		//				glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
		//				glPopMatrix();
		//			}
		//			
		//			glBindTexture(GL_TEXTURE_2D, 0);
		//		}
		//		
		
		private readonly float[] quadVertices = {
			-1,-1,0,  1,-1,0, -1, 1,0,  1, 1,0
		};
		
		private readonly float[] quadTexCoords = {
			1, 1,  1, 0,  0, 1,  0, 0 
		};	
		
		private void DrawInstr ()
		{
			GL1.Color4 (1.0f, 1.0f, 1.0f, 1.0f);
			
			GL1.VertexPointer (3, All1.Float, 0, quadVertices);
			GL1.TexCoordPointer (2, All1.Float, 0, quadTexCoords);
			
			// draw the info button
			GL1.BindTexture (All1.Texture2D, texture [2]);
			GL1.PushMatrix ();
			GL1.Translate (-1.0f, 1.5f, 0.0f); //move to the bottom-left corner (in landscape)
			GL1.Scale (0.05f, 0.05f, 1.0f);
			GL1.Translate (2.0f, -1.5f, 0.0f);
			GL1.DrawArrays (All1.TriangleStrip, 0, 4);
			GL1.PopMatrix ();
			
			// draw text
			GL1.BindTexture (All1.Texture2D, texture [3]);
			GL1.PushMatrix ();
			GL1.Translate (-1.0f, 1.3f, 0.0f); //move to the bottom-left corner (in landscape)
			GL1.Scale (0.1f, 0.4f, 1.0f);
			GL1.Translate (1.0f, -1.0f, 0.0f);
			GL1.DrawArrays (All1.TriangleStrip, 0, 4);
			GL1.PopMatrix ();
			
			GL1.BindTexture (All1.Texture2D, 0);
			
			if (showDesc) {
				// put description in front of everything
				float[] descVertices = {
					-1,-1,10,  1,-1,10, -1, 1,10,  1, 1,10
				};
				
				float w = 480f / 512f, h = 198f / 256f;
				float[] descTexCoords = {
					w, h,  w, 0,  0, h,  0, 0 
				};
				
				GL1.VertexPointer (3, All1.Float, 0, descVertices);
				GL1.TexCoordPointer (2, All1.Float, 0, descTexCoords);
				
				// draw transparent gray in background
				GL1.Color4 (0.05f, 0.05f, 0.05f, 0.8f);
				GL1.PushMatrix ();
				GL1.Translate (0.0f, 0.0f, -0.1f);
				GL1.Scale (1.0f, 1.5f, 1.0f);
				GL1.DrawArrays (All1.TriangleStrip, 0, 4);
				GL1.PopMatrix ();
				
				// draw description text on top
				GL1.Color4 (1.0f, 1.0f, 1.0f, 1.0f);
				GL1.BindTexture (All1.Texture2D, texture [4]);
				GL1.PushMatrix ();
				GL1.Scale (198f / 320f, 1.5f, 1.0f);
				GL1.DrawArrays (All1.TriangleStrip, 0, 4);
				GL1.PopMatrix ();
				
				GL1.BindTexture (All1.Texture2D, 0);
			}
		}
		//		- (void)drawInstr
		//		{	
		//			const GLfloat quadVertices[] = {
		//				-1,-1,0,  1,-1,0, -1, 1,0,  1, 1,0
		//			};
		//			
		//			const GLfloat quadTexCoords[] = {
		//				1, 1,  1, 0,  0, 1,  0, 0 
		//			};	
		//			
		//			glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
		//			
		//			glVertexPointer(3, GL_FLOAT, 0, quadVertices);
		//			glTexCoordPointer(2, GL_FLOAT, 0, quadTexCoords);
		//			
		//			// draw the info button
		//			glBindTexture(GL_TEXTURE_2D, texture[2]);
		//			glPushMatrix();
		//			glTranslatef(-1.0f, 1.5f, 0.0f); //move to the bottom-left corner (in landscape)
		//			glScalef(0.05f, 0.05f, 1.0f);
		//			glTranslatef(2.0f, -1.5f, 0.0f);
		//			glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
		//			glPopMatrix();
		//			
		//			// draw text
		//			glBindTexture(GL_TEXTURE_2D, texture[3]);
		//			glPushMatrix();
		//			glTranslatef(-1.0f, 1.3f, 0.0f); //move to the bottom-left corner (in landscape)
		//			glScalef(0.1f, 0.4f, 1.0f);
		//			glTranslatef(1.0f, -1.0f, 0.0f);
		//			glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
		//			glPopMatrix();
		//			
		//			glBindTexture(GL_TEXTURE_2D, 0);
		//			
		//			if (showDesc)
		//			{
		//				// put description in front of everything
		//				const GLfloat descVertices[] = {
		//					-1,-1,10,  1,-1,10, -1, 1,10,  1, 1,10
		//				};
		//				
		//				GLfloat w = 480./512., h = 198./256.;
		//				const GLfloat descTexCoords[] = {
		//					w, h,  w, 0,  0, h,  0, 0 
		//				};
		//				
		//				glVertexPointer(3, GL_FLOAT, 0, descVertices);
		//				glTexCoordPointer(2, GL_FLOAT, 0, descTexCoords);
		//				
		//				// draw transparent gray in background
		//				glColor4f(0.05f, 0.05f, 0.05f, 0.8f);
		//				glPushMatrix();
		//				glTranslatef(0.0f, 0.0f, -0.1f);
		//				glScalef(1.0f, 1.5f, 1.0f);
		//				glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
		//				glPopMatrix();
		//				
		//				// draw description text on top
		//				glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
		//				glBindTexture(GL_TEXTURE_2D, texture[4]);
		//				glPushMatrix();
		//				glScalef(198./320., 1.5f, 1.0f);
		//				glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
		//				glPopMatrix();
		//				
		//				glBindTexture(GL_TEXTURE_2D, 0);
		//			}
		//		}	
		
		private void DrawView ()
		{
			GL1.ClearColor (0.0f, 0.0f, 0.0f, 1.0f);
			GL1.ClearDepth (1.0f);
			GL1.Clear ((int)All1.ColorBufferBit | (int)All1.DepthBufferBit);
			
			// start drawing 3D objects
			GL1.Enable (All1.DepthTest);
			GL1.MatrixMode (All1.Projection);
			GL1.LoadIdentity ();
			GL1.Ortho (-1.0f, 1.0f, -1.5f, 1.5f, -10.0f, 10.0f);
			// tranform the camara for a better view
			GL1.Translate (0.07f, 0.0f, 0.0f);
			GL1.Rotate (-30.0f, 0.0f, 1.0f, 0.0f);
			GL1.MatrixMode (All1.Modelview);
			
			this.DrawCircle (innerCircleVertices, kCircleSegments);
			this.DrawCircle (outerCircleVertices, kCircleSegments);
			this.DrawTeapot ();
			
			// enable GL states for texturing
			// this includes cube and 2D instructions and buttons
			GL1.EnableClientState (All1.VertexArray);
			GL1.EnableClientState (All1.TextureCoordArray);
			GL1.Enable (All1.Texture2D);
			
			this.DrawCube ();
			
			if (!showDesc)
				GL1.Disable (All1.DepthTest);
			
			// start drawing 2D instructions and buttons
			GL1.MatrixMode (All1.Projection);
			GL1.LoadIdentity ();
			GL1.Ortho (-1.0f, 1.0f, -1.5f, 1.5f, -10.0f, 10.0f);
			GL1.MatrixMode (All1.Modelview);
			
			GL1.Enable (All1.Blend);
			GL1.BlendFunc (All1.One, All1.OneMinusSrcAlpha);
			this.DrawModes ();
			this.DrawInstr ();
			GL1.Disable (All1.Blend);
			
			//disable GL states for texturing
			GL1.DisableClientState (All1.VertexArray);
			GL1.DisableClientState (All1.TextureCoordArray);
			GL1.Disable (All1.Texture2D);
			
			if (showDesc)
				GL1.Disable (All1.DepthTest);
		}
		//		- (void)drawView {
		//			
		//			[EAGLContext setCurrentContext:context];
		//			
		//			glBindFramebufferOES(GL_FRAMEBUFFER_OES, viewFramebuffer);
		//			
		//			glClearColor(0.0f, 0.0f, 0.0f, 1.0f);
		//			glClearDepthf(1.0f);
		//			glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
		//			
		//			// start drawing 3D objects
		//			glEnable(GL_DEPTH_TEST);
		//			glMatrixMode(GL_PROJECTION);
		//			glLoadIdentity();
		//			glOrthof(-1.0f, 1.0f, -1.5f, 1.5f, -10.0f, 10.0f);
		//			// tranform the camara for a better view
		//			glTranslatef(0.07f, 0.0f, 0.0f);
		//			glRotatef(-30.0f, 0.0f, 1.0f, 0.0f);
		//			glMatrixMode(GL_MODELVIEW);
		//			
		//			[self drawCircle:innerCircleVertices withNumOfSegments:kCircleSegments];
		//			[self drawCircle:outerCircleVertices withNumOfSegments:kCircleSegments];
		//			[self drawTeapot];
		//			
		//			// enable GL states for texturing
		//			// this includes cube and 2D instructions and buttons
		//			glEnableClientState(GL_VERTEX_ARRAY);
		//			glEnableClientState(GL_TEXTURE_COORD_ARRAY);
		//			glEnable(GL_TEXTURE_2D);
		//			
		//			[self drawCube];
		//			
		//			if (!showDesc) glDisable(GL_DEPTH_TEST);
		//			
		//			// start drawing 2D instructions and buttons
		//			glMatrixMode(GL_PROJECTION); 
		//			glLoadIdentity();
		//			glOrthof(-1.0f, 1.0f, -1.5f, 1.5f, -10.0f, 10.0f);
		//			glMatrixMode(GL_MODELVIEW);
		//			
		//			glEnable(GL_BLEND);
		//			glBlendFunc(GL_ONE, GL_ONE_MINUS_SRC_ALPHA);
		//			[self drawModes];
		//			[self drawInstr];
		//			glDisable(GL_BLEND);
		//			
		//			//disable GL states for texturing
		//			glDisableClientState(GL_VERTEX_ARRAY);
		//			glDisableClientState(GL_TEXTURE_COORD_ARRAY);
		//			glDisable(GL_TEXTURE_2D);
		//			
		//			if (showDesc) glDisable(GL_DEPTH_TEST);
		//			
		//			glBindRenderbufferOES(GL_RENDERBUFFER_OES, viewRenderbuffer);
		//			[context presentRenderbuffer:GL_RENDERBUFFER_OES];
		//		}
		
		#endregion
		
		#region TouchEvents
		
		public override void TouchesEnded (NSSet touches, UIEvent evt)
		{
			UITouch touch = (UITouch)(touches.Count == 1 ? touches.AnyObject : null);
			
			if (touch != null) {
				// Convert touch point from UIView referential to OpenGL one (upside-down flip)
				System.Drawing.RectangleF bounds = this.Bounds;
				System.Drawing.PointF location = touch.LocationInView (this);
				location.Y = bounds.Size.Height - location.Y;
				
				if (!showDesc) {
					// Compute the bounds of the four buttons (1,2,3,4)
					// for 2D drawing,  projection transform is set to glOrthof(-1.0f, 1.0f, -1.5f, 1.5f, -10.0f, 10.0f);
					int buttonSize = Convert.ToInt32 (kButtonScale * this.Size.Width);
					int xmin, xmax, ymin, ymax;
					xmin = 0;
					xmax = buttonSize;
					ymax = Convert.ToInt32 ((1 - kButtonLeftSpace / 3.0) * this.Size.Height);
					ymin = ymax - buttonSize * 4;
					
					// if touch point is in the bounds, compute the selected mode
					if (location.X >= xmin && location.X < xmax && location.Y >= ymin && location.Y < ymax) {
						int m = Convert.ToInt32 ((location.Y - ymin) / buttonSize);
						// clamp to 0~3
						m = m < 0 ? 0 : (m > 3 ? 3 : m);
						// our mode is 1~4
						// invert, as the bigger y is, the smaller mode is
						m = 4 - m;
						
						// switch to the new mode and update parameters
						if (mode != m) {
							mode = (uint)m;
							
							// update the position of the cube (sound source)
							// in mode 1 and 2, the teapot (sound source) is at the center of the sound stage
							// in mode 3 and 4, the teapot (sound source) is on the left side
							if (mode <= 2) {
								cubePos [0] = cubePos [1] = cubePos [2] = 0;
							} else {
								cubePos [0] = 0; 
								cubePos [1] = (kInnerCircleRadius + kOuterCircleRadius) / 2.0f;
								cubePos [2] = 0;
							}
							
							// update playback
							playback.SourcePos = cubePos; //sound source's position
						}
					}
					
					// user touches the info icon at the corner when playing
					else if (location.X >= 0 && location.X < 40 && location.Y >= 440 && location.Y < 480) {
						// show the description and pause
						showDesc = true;
						playback.StopSound ();
					}
				}
				
				// user touches anywhere on the screen when pausing
				else {
					// dismiss the description and continue
					showDesc = false;
					playback.StartSound ();
				}
			}
		}
		//		- (void)touchesEnded:(NSSet *)touches withEvent:(UIEvent *)event
		//		{
		//			[super touchesEnded:touches withEvent:event];
		//			
		//			UITouch* touch = ([touches count] == 1 ? [touches anyObject] : nil);
		//			
		//			if (touch)
		//			{
		//				// Convert touch point from UIView referential to OpenGL one (upside-down flip)
		//				CGRect bounds = [self bounds];
		//				CGPoint location = [touch locationInView:self];
		//				location.y = bounds.size.height - location.y;
		//				
		//				if (!showDesc)
		//				{
		//					// Compute the bounds of the four buttons (1,2,3,4)
		//					// for 2D drawing,  projection transform is set to glOrthof(-1.0f, 1.0f, -1.5f, 1.5f, -10.0f, 10.0f);
		//					GLint buttonSize = kButtonScale*backingWidth;
		//					GLint xmin, xmax, ymin, ymax;
		//					xmin = 0;
		//					xmax = buttonSize;
		//					ymax = (1 - kButtonLeftSpace/3.0) * backingHeight;
		//					ymin = ymax - buttonSize*4;
		//					
		//					// if touch point is in the bounds, compute the selected mode
		//					if (location.x >= xmin && location.x < xmax && location.y >= ymin && location.y < ymax)
		//					{
		//						GLint m = (location.y - ymin) / buttonSize;
		//						// clamp to 0~3
		//						m = m<0 ? 0 : (m>3 ? 3 : m);
		//						// our mode is 1~4
		//						// invert, as the bigger y is, the smaller mode is
		//						m = 4-m;
		//						
		//						// switch to the new mode and update parameters
		//						if (mode != m)
		//						{
		//							mode = m;
		//							
		//							// update the position of the cube (sound source)
		//							// in mode 1 and 2, the teapot (sound source) is at the center of the sound stage
		//							// in mode 3 and 4, the teapot (sound source) is on the left side
		//							if (mode <= 2) {
		//								cubePos[0] = cubePos[1] = cubePos[2] = 0;
		//							}
		//							else {
		//								cubePos[0] = 0; 
		//								cubePos[1] = (kInnerCircleRadius + kOuterCircleRadius) / 2.0;
		//								cubePos[2] = 0;
		//							}
		//							
		//							// update playback
		//							playback.sourcePos = cubePos; //sound source's position
		//						}
		//					}
		//					
		//					// user touches the info icon at the corner when playing
		//					else if (location.x >= 0 && location.x < 40 && location.y >= 440 && location.y < 480)
		//					{
		//						// show the description and pause
		//						showDesc = YES;
		//						[playback stopSound];
		//					}
		//				}
		//				
		//				// user touches anywhere on the screen when pausing
		//				else
		//				{
		//					// dismiss the description and continue
		//					showDesc = NO;
		//					[playback startSound];
		//				}
		//			}
		//		}
		
		#endregion
		
	}
	
}
