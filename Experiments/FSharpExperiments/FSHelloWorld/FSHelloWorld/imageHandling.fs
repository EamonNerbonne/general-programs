
#light
module NativeImageHandler


open System
open Microsoft.FSharp.NativeInterop
open System.Drawing
open System.Drawing.Imaging 

type RgbColor =
  

let fromBitmap (image:Bitmap) =
   let bd = image.LockBits(Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb)
   try
     //each pixel is 32 bits so this is what we're looking for here...
     let ptr = NativePtr.of_nativeint<uint32> bd.Scan0
     let pixelByteLen = sizeof<uint32>
     let rowByteLen = bd.Stride / pixelByteLen
     let getPixel x y =
         NativePtr.get ptr (x + y*rowByteLen) 
     let convertPixel ui =
        byte (ui&&& 0x00ff0000u >>>16), byte (ui&&& 0x0000ff00u >>>8), byte (ui&&& 0x000000ffu)
     let extractPixel x y = convertPixel(getPixel x y)
     Array2.init bd.Width bd.Height extractPixel
   finally  
     image.UnlockBits(bd)
   
   // Return the array
   

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

