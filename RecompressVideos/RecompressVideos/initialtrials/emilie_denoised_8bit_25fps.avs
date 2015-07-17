FFVideoSource("emilie2.mts")
ColorYUV(gain_y=90,off_y=-16,off_v=-26,off_u=10,cont_u=32,cont_v=32)
#ColorYUV(analyze=true)
#Trim(0,500)
ConvertToYV12
MCTD(settings="medium",radius=5,sigma=20,bt=5,bwbh=32,owoh=16,enhance=true)
SelectEven()
#TODO: tune sigma
#,dbF="GradFun3(lsb=true,radius=16,smode=2)"
#Dither_out() #required for x264



