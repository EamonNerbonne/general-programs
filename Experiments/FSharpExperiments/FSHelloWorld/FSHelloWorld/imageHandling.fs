
#light
open System
open Microsoft.FSharp.NativeInterop
open System.Drawing
open System.Drawing.Imaging 

module NativeImageHandler = begin

 // Average the three bytes at the pointer (r,g,b)
 let getAvgPosition p  =
      (int16 (NativePtr.get p 0) +
       int16 (NativePtr.get p 1) +
       int16 (NativePtr.get p 2))
       / (int16 3)

 let fromBitmap (image:Bitmap) =
   // Create the array
   let (arr2:int16[,]) = Array2.create image.Height image.Width (int16 0)
   let bd = image.LockBits(Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb)
   let mutable (p:nativeptr<byte>) = NativePtr.of_nativeint (bd.Scan0)
   for i=0 to image.Height-1 do
     for j=0 to image.Width-1 do
      // Populate our 2 dim array with the average of the rgb bytes.
      arr2.[i,j] <- getAvgPosition p
      // Go to the next position
      p <- NativePtr.add p 4
     done
     // The stride - the whole length (multiplied by four to account
     // for the fact that we are looking at 4 byte pixels
     p <- NativePtr.add p (bd.Stride - bd.Width*4)
   done
   // Unlock the image bytes
   image.UnlockBits(bd)
   // Return the array
   arr2

 /// Sets the three bytes following the given pointer to v
 let private setPosition p i j v =
      NativePtr.set p 0 (byte v)
      NativePtr.set p 1 (byte v)
      NativePtr.set p 2 (byte v) 

 let toBitmap (arr:int16[,]) =
   // Create the bitmap
   let image = new Bitmap(arr.GetLength(1),arr.GetLength(0))
   // Get the bitmap data for a 32 bpp bitmap with a Read Write lock
   let bd = image.LockBits(Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb)
   // Setup the pointer
   let mutable (p:nativeptr<byte>) = NativePtr.of_nativeint (bd.Scan0)
   //
   for i=0 to image.Height-1 do
     for j=0 to image.Width-1 do
       setPosition p i j (arr.[i,j])
       p <- NativePtr.add p 4
     done
     // The stride - the whole length (multiplied by four to account
     // for the fact that we are looking at 4 byte pixels
     p <- NativePtr.add p (bd.Stride - bd.Width*4)
   done
   // Unlock the image bytes
   image.UnlockBits(bd)
   image

end
