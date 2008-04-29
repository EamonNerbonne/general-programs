#light 
#I @"C:\Program Files\Reference Assemblies\Microsoft\Framework\v3.0"
#r @"WindowsBase.dll"
#r @"PresentationCore.dll"
#r @"PresentationFramework.dll"

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Drawing

let flattenArr2 (arr2:'a[,]) =
  let dim0=arr2.GetLength(0)
  let dim1=arr2.GetLength(1)
  let base0,base1 = ( arr2.GetLowerBound(0), arr2.GetLowerBound(1))
  let retval = Array.create (dim0*dim1) arr2.[base0,base1] 
  arr2 |> Array2.iteri (fun i j V -> retval.[j-base1+(i-base0)*dim1] <- V );
  retval



type ShowMyBitmap = class
   inherit Window as base  
   
   new (img:uint16[,]) as this = {} then
      this.Title <- "Show My Pic"
      let width = img.GetLength(1)
      let height = img.GetLength(0)
      printfn "w=%d, h=%d" width height
      let bitmapImage = BitmapSource.Create(width, height, 
                                            96.0, 96.0, PixelFormats.Gray8,null,Array.map byte (flattenArr2 img),img.GetLength(1));
      let controlImg = new Controls.Image()
      controlImg.Source <- bitmapImage
      this.Content <- controlImg
      
end
