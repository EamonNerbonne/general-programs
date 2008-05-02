#light 

module ShowImg
#I @"C:\Program Files\Reference Assemblies\Microsoft\Framework\v3.0"
#r @"WindowsBase.dll"
#r @"PresentationCore.dll"
#r @"PresentationFramework.dll"
#I @"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\EmnImaging\EmnImaging\bin\Debug"
#R @"EmnImaging.dll"


open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Drawing
open EmnImaging

let flattenArr2 (arr2:'a[,]) =
  let dim0=arr2.GetLength(0)
  let dim1=arr2.GetLength(1)
  let base0,base1 = ( arr2.GetLowerBound(0), arr2.GetLowerBound(1))
  let retval = Array.create (dim0*dim1) arr2.[base0,base1] 
  arr2 |> Array2.iteri (fun i j V -> retval.[j-base1+(i-base0)*dim1] <- V );
  retval



type ShowMyBitmap = class
   inherit Window as base  
   
   new (img:EmnImaging.PixelArgb32[,]) as this = {} then
      this.Title <- "Show My Bitmap"
      let width = img.GetLength(0)
      let height = img.GetLength(1)
      let dataArr = Array.init (width*height*4) (fun i ->
        let offset = i%4
        let x = (i/4) % width
        let y = (i/4/width)
        let pixel = img.[x,y]
        match offset with
          | 0 -> pixel.B
          | 1 -> pixel.G
          | 2 -> pixel.R
          | 3 -> pixel.A
          | _ -> failwith "impossible: i%4 must be 0, 1, 2 or 3."
        )
      
      let bitmapImage = BitmapSource.Create(width, height, 
                                            96.0, 96.0, PixelFormats.Bgra32,null,dataArr,width*4);
      let controlImg = new Controls.Image()
      controlImg.Source <- bitmapImage
      this.Content <- controlImg
      
end
