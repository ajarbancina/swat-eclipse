      subroutine res
      
!!    ~ ~ ~ PURPOSE ~ ~ ~
!!    this subroutine routes water and sediment through reservoirs
!!    computes evaporation and seepage from the reservoir.

!!    ~ ~ ~ INCOMING VARIABLES ~ ~ ~
!!    name         |units         |definition
!!    ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ 
!!    br1(:)       |none          |1st shape parameter for reservoir surface
!!                                |area equation
!!    br2(:)       |none          |2nd shape parameter for reservoir surface
!!                                |area equation
!!    curyr        |none          |current year of simulation
!!    evrsv(:)     |none          |lake evaporation coefficient
!!    iflod1r(:)   |none          |beginning month of non-flood season
!!                                |(needed if IRESCO=2)
!!    iflod2r(:)   |none          |ending month of non-flood season
!!                                |(needed if IRESCO=2)
!!    inum1        |none          |reservoir number
!!    iresco(:)    |none          |outflow simulation code:
!!                                |0 compute outflow for uncontrolled reservoir
!!                                |  with average annual release rate
!!                                |1 measured monthly outflow
!!                                |2 simulated controlled outflow-target release
!!                                |3 measured daily outflow
!!    i_mo         |none          |current month of simulation
!!    ndtargr(:)   |days          |number of days to reach target storage from
!!                                |current reservoir storage
!!                                |(needed if IRESCO=2)
!!    oflowmn(:,:) |m^3/day       |minimum daily ouflow for the month
!!    oflowmx(:,:) |m^3/day       |maximum daily ouflow for the month
!!    pet_day      |mm H2O        |potential evapotranspiration on day
!!    res_evol(:)  |m**3          |volume of water needed to fill the reservoir
!!                                |to the emergency spillway
!!    res_k(:)     |mm/hr         |hydraulic conductivity of the reservoir 
!!                                |bottom
!!    res_nsed(:)  |kg/L          |normal amount of sediment in reservoir
!!    res_pvol(:)  |m**3          |volume of water needed to fill the reservoir
!!                                |to the principal spillway 
!!    res_rr(:)    |m**3/day      |average daily principal spillway release
!!                                |volume
!!    res_sed(:)   |kg/L (ton/m^3)|amount of sediment in reservoir
!!    res_sub(:)   |none          |number of subbasin reservoir is in
!!    res_vol(:)   |m^3 H2O       |reservoir volume
!!    resflwi      |m^3 H2O       |water entering reservoir on day
!!    res_out(:,:,:)|m**3/day      |measured average daily outflow from the
!!                                |reservoir for the month
!!    ressedi      |metric tons   |sediment entering reservoir during time step
!!    sub_subp(:)  |mm H2O        |precipitation for day in subbasin
!!    sub_sumfc(:) |mm H2O        |amount of water in subbasin soil at field 
!!                                |capacity
!!    sub_sw(:)    |mm H2O        |amount of water in soil profile in subbasin
!!    starg(:,:)   |m**3          |monthly target reservoir storage
!!    wuresn(:,:)  |m**3          |average amount of water withdrawn from
!!                                |reservoir each month for consumptive water 
!!                                |use
!!    wurtnf(:)    |none          |fraction of water removed from the reservoir
!!                                |via WURESN which is returned and becomes flow
!!                                |from the reservoir outlet
!!    ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ 

!!    ~ ~ ~ OUTGOING VARIABLES ~ ~ ~
!!    name        |units         |definition
!!    ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ 
!!    res_sed(:)  |kg/L (ton/m^3)|amount of sediment in reservoir
!!    res_vol(:)  |m^3 H2O       |reservoir volume
!!    resev       |m^3 H2O       |evaporation from reservoir on day
!!    resflwo     |m^3 H2O       |water leaving reservoir on day
!!    respcp      |m^3 H2O       |precipitation on reservoir for day
!!    ressa       |ha            |surface area of reservoir on day
!!    ressep      |m^3 H2O       |seepage from reservoir on day
!!    ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ 

!!    ~ ~ ~ LOCAL DEFINITIONS ~ ~ ~
!!    name        |units         |definition
!!    ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ 
!!    flw         |m^3/s         |reservoir outflow for day
!!    jres        |none          |reservoir number
!!    sed         |kg/L          |concentration of sediment in reservoir at
!!                               |beginning of day
!!    targ        |m^3 H2O       |target reservoir volume for day
!!    vol         |m^3 H2O       |volume of water stored in reservoir at 
!!                               |beginning of day
!!    vvr         |m^3 H2O       |maximum controlled water release for day
!!    xx          |none          |variable to hold intermediate calculation 
!!                               |result
!!    ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ 

!!    ~ ~ ~ SUBROUTINES/FUNCTIONS CALLED ~ ~ ~
!!    Intrinsic: Min

!!    ~ ~ ~ ~ ~ ~ END SPECIFICATIONS ~ ~ ~ ~ ~ ~

      use parm

      integer :: jres
      real :: vol, sed, vvr, targ, xx, flw
	real :: san,sil,cla,sag,lag,gra
	real :: inised, finsed, setsed, remsetsed
      
      integer :: ipt1,ipt2, i24  !liu
      real :: volmean, voltmp  !liu
      real :: vol1,vol2,vol3,vol4,vol5,vol0,diff,diff0      !liu yb     
      diff0 = 50.0  !liu yb
      jres = 0
      jres = inum1

!! store initial values
      vol = 0.
      sed = 0.
	inised = 0.
	finsed = 0.
	setsed = 0.
	remsetsed = 0.

      vol = res_vol(jres)
      sed = res_sed(jres)

!!    Storage by particle sizes
      san = res_san(jres)
      sil = res_sil(jres)
      cla = res_cla(jres)
      sag = res_sag(jres)
      lag = res_lag(jres)
	gra = res_gra(jres)

!! calculate surface area for day
    !  ressa = br1(jres) * res_vol(jres) ** br2(jres)

      if (iresco(jres)<4) then                           !liu
       ressa = br1(jres) * res_vol(jres) ** br2(jres)
	else                                               !liu
!du tmep>
      if (iida==154.and.inum1==1) then
        ressa=0.0
      end if
      if (inum1==1) then
        ressa=0.0
      end if
!liu temp
!liu>
   !   i24max=24
   !   voltmp=0.0
   !   ressa=0.0
      
   !   volmean=vol
   !   do i24=1,i24max
      
   !   volmean=volmean+resflwi/i24max
      
   !	  If (volmean < resv(1,jres)) then
   !	   resflwo = resq(1,jres)
   !    elseif (volmean > resv(mpoint(jres),jres)) then
   !	    resflwo = resq(mpoint(jres),jres)     
   !	  else
	  
   !	   do ipt1=mpoint(jres)-1,1,-1 
   !	    if(volmean >= resv(ipt1,jres)) then
   !	 	 xx=resv(ipt1+1,jres)-resv(ipt1,jres)
   !	 	 if (xx>0) xx=(volmean- resv(ipt1,jres))/xx
   !		resflwo=resq(ipt1,jres)+(resq(ipt1+1,jres)-resq(ipt1,jres))*xx
   !	    exit
   !	    end if
   !	   end do
   !	  end if

   !     voltmp=voltmp+resflwo
   !     volmean=volmean-resflwo*86400.0/i24max
	  
   !   end do


   !     resflwo=voltmp/i24max
	  
   !     !calculate the water area of the end of day
   !	   do ipt1=mpoint(jres)-1,1,-1 
   !	    if(volmean >= resv(ipt1,jres)) then
   !	 	 xx=resv(ipt1+1,jres)-resv(ipt1,jres)
   !	 	 if (xx>0) xx=(volmean- resv(ipt1,jres))/xx
   !		ressa=resa(ipt1,jres)+(resa(ipt1+1,jres)-resa(ipt1,jres))*xx
   !	    exit
   !	    end if
   !	   end do
   !	 resflwo=resflwo*86400.0
   !	end if
!liu<

!liu yb> 
      volmean=vol+resflwi   
 	  If (volmean <= resv(1,jres)) then
 	   ressa = resa(1,jres)
 	   resflwo = 0.0
         
      !   if (jres==17 .and. volmean<10888) then
      !   ressa = 0.2
 	!   resflwo = 0.0
      !   endif

 	  elseif (volmean > resv(mpoint(jres),jres)) then
 	    ressa = resa(mpoint(jres),jres)
 	    resflwo = resflwi/86400.0

 	  else
          
          vol0 = 0.0
          vol1 = vol0
          vol2 = volmean*2.
111       vol3 = (vol1 + vol2)/2.0  
 	   do ipt1=mpoint(jres)-1,1,-1 
 	    if(vol3 >= resv(ipt1,jres)) then
 	 	 xx=resv(ipt1+1,jres)-resv(ipt1,jres)
 	 	 if (xx>0) xx=(vol3- resv(ipt1,jres))/xx
 		ressa=resa(ipt1,jres)+(resa(ipt1+1,jres)-resa(ipt1,jres))*xx
 		resflwo=resq(ipt1,jres)+(resq(ipt1+1,jres)-resq(ipt1,jres))*xx    
          exit
 	    end if
 	   end do

        vol4 = volmean - resflwo*86400.
        diff = vol3-vol4
        if (abs(diff) > diff0) then
           if (diff>0.0) then
           vol1 = vol0
           vol2 = vol3
           vol5 = vol3
           goto 111
           
           else
           vol1 = vol5
           vol2 = vol3
           vol0 = vol3
           goto 111
           end if
        end if 
       end if
       resflwo=resflwo*86400.0
!liu yb< 
       end if

!! calculate water balance for day
      resev = 10. * evrsv(jres) * pet_day * ressa
      ressep = res_k(jres) * ressa * 240.
      respcp = sub_subp(res_sub(jres)) * ressa * 10.

!! new water volume for day
      res_vol(jres) = res_vol(jres) + respcp + resflwi - resev - ressep

!! if reservoir volume is greater than zero
     
        !! determine reservoir outflow
        select case (iresco(jres))
          case (0)                    !! uncontrolled reservoir
            vvr = 0.
            if (res_vol(jres) > res_pvol(jres)) then
              vvr = res_vol(jres) - res_pvol(jres)
              if (res_vol(jres) > res_evol(jres)) then
                resflwo = res_vol(jres) - res_evol(jres)
                vvr = res_evol(jres) - res_pvol(jres)
              endif
              if (res_rr(jres) > vvr) then
                resflwo = resflwo + vvr
              else
                resflwo = resflwo + res_rr(jres)
              endif
            endif

          case (1)                   !! use measured monthly outflow
            resflwo = res_out(jres,i_mo,curyr)

          case (2)                   !! controlled outflow-target release
            targ = 0.
            if (starg(i_mo,jres) > 0.) then
              targ = starg(i_mo,jres)
            else
              !! target storage based on flood season and soil water
              if (iflod2r(jres) > iflod1r(jres)) then
                if (i_mo > iflod1r(jres) .and. i_mo < iflod2r(jres))    &
     &                                                              then
                  targ = res_evol(jres)
                else
                xx = Min(sub_sw(res_sub(jres))/sub_sumfc(res_sub(jres)),&
     &                                                               1.)
                targ = res_pvol(jres) + .5 * (1. - xx) *                &
     &                                 (res_evol(jres) - res_pvol(jres))
                end if
              else
                if (i_mo > iflod1r(jres) .or. i_mo < iflod2r(jres)) then
                  targ = res_evol(jres)
                else
                xx = Min(sub_sw(res_sub(jres))/sub_sumfc(res_sub(jres)),&
     &                                                               1.)
                targ = res_pvol(jres) + .5 * (1. - xx) *                &
     &                                 (res_evol(jres) - res_pvol(jres))
                end if
              end if
            endif
            if (res_vol(jres) > targ) then
              resflwo = (res_vol(jres) - targ) / ndtargr(jres)
            else
              resflwo = 0.
            end if

          case (3)                   !! use measured daily outflow
            flw = 0.
            read (350+jres,5000) flw
            resflwo = 86400. * flw
          case (4)
!>liu
!            targ = res_pvol(jres) * starg_fps(jres)
!            if (res_vol(jres) > targ) then
!              resflwo = (res_vol(jres) - targ) / ndtargr(jres)
!            else
!              resflwo = 0.
!            end if
!            if (resflwo < oflowmn_fps(jres)) resflwo = oflowmn_fps(jres)
!<liu

        end select
         
!! if reservoir volume is zero
      if (res_vol(jres) < 0.001) then

        !! if volume deficit in reservoir exists, reduce seepage so
        !! that reservoir volume is zero
        ressep = ressep + res_vol(jres)
        res_vol(jres) = 0.

        !! if seepage is less than volume deficit, take remainder
        !! from evaporation
        if (ressep < 0.) then
          resev = resev + ressep
          ressep = 0.
        end if
        res_sed(jres) = 0.

      else

        !! compute new sediment concentration in reservoir
	if (ressedi < 1.e-6) ressedi = 0.0      !!nbs 02/05/07
        res_sed(jres) = (ressedi + sed * vol) / res_vol(jres)
        res_san(jres) = (ressani + san * vol) / res_vol(jres)
        res_sil(jres) = (ressili + sil * vol) / res_vol(jres)
        res_cla(jres) = (resclai + cla * vol) / res_vol(jres)
        res_sag(jres) = (ressagi + sag * vol) / res_vol(jres)
        res_lag(jres) = (reslagi + lag * vol) / res_vol(jres)
        res_gra(jres) = (resgrai + gra * vol) / res_vol(jres)

        res_sed(jres) = Max(0.,res_sed(jres))
        res_san(jres) = Max(0.,res_san(jres))
        res_sil(jres) = Max(0.,res_sil(jres))
        res_cla(jres) = Max(0.,res_cla(jres))
        res_sag(jres) = Max(0.,res_sag(jres))
        res_lag(jres) = Max(0.,res_lag(jres))
        res_gra(jres) = Max(0.,res_gra(jres))

        
        !liu
                !! compute sediment leaving reservoir
        ressedo = res_sed(jres) * resflwo
        ressano = res_san(jres) * resflwo
        ressilo = res_sil(jres) * resflwo
        resclao = res_cla(jres) * resflwo
        ressago = res_sag(jres) * resflwo
        reslago = res_lag(jres) * resflwo
	  resgrao = res_gra(jres) * resflwo
        !Liu
        
        !! check calculated outflow against specified max and min values
   !     if (resflwo < oflowmn(i_mo,jres)) resflwo = oflowmn(i_mo,jres)
   !     if (resflwo > oflowmx(i_mo,jres) .and. oflowmx(i_mo,jres) > 0.) &
   ! &                                                              then
   !       resflwo = oflowmx(i_mo,jres)
   !     endif
        
        if (iresco(jres)<4) then                                          !liu 
        if (resflwo < oflowmn(i_mo,jres)) resflwo = oflowmn(i_mo,jres)
        if (resflwo > oflowmx(i_mo,jres) .and. oflowmx(i_mo,jres) > 0.) &
     &                                                              then
          resflwo = oflowmx(i_mo,jres)
        endif
      end if  
 !     print*,  resflwo       
        !! subtract outflow from reservoir storage
        res_vol(jres) = res_vol(jres) - resflwo
        if (res_vol(jres) < 0.) then
          resflwo = resflwo + res_vol(jres)
          res_vol(jres) = 0.
        end if

!liu>
       if (res_vol(jres)<resdead(jres)) then 
        if (resflwo>resdead(jres)) then 
         resflwo=resflwo-(resdead(jres)-res_vol(jres))
         res_vol(jres)=resdead(jres)
        elseif (resflwo>0.) then
         res_vol(jres)=res_vol(jres)+resflwo
         if (res_vol(jres)>resdead(jres)) then
          resflwo=res_vol(jres)-resdead(jres)
          res_vol(jres)=resdead(jres)
         else
          resflwo=0.
         endif
        end if
       end if
!liu<


        !! subtract consumptive water use from reservoir storage
        xx = 0.
        xx = wuresn(i_mo,jres)
        res_vol(jres) = res_vol(jres) - xx
        if (res_vol(jres) < 0.) then
          xx = xx + res_vol(jres)
          res_vol(jres) = 0.
        end if
        !! add spillage from consumptive water use to reservoir outflow
        resflwo = resflwo + xx * wurtnf(jres)

        !! compute change in sediment concentration due to settling
        if (res_sed(jres) < 1.e-6) res_sed(jres) = 0.0    !!nbs 02/05/07
        if (res_sed(jres) > res_nsed(jres)) then
	    inised = res_sed(jres)
          res_sed(jres) = (res_sed(jres) - res_nsed(jres)) *            &
     &                                   sed_stlr(jres) + res_nsed(jres)
	    finsed = res_sed(jres)
	    setsed = inised - finsed

        if (res_gra(jres) >= setsed) then
	    res_gra(jres) = res_gra(jres) - setsed
	    remsetsed = 0.
	  else
	    remsetsed = setsed - res_gra(jres)
          res_gra(jres) = 0.
		if (res_lag(jres) >= remsetsed) then
	      res_lag(jres) = res_lag(jres) - remsetsed
	      remsetsed = 0.
	    else
	      remsetsed = remsetsed - res_lag(jres)
	      res_lag(jres) = 0.
	      if (res_san(jres) >= remsetsed) then
	        res_san(jres) = res_san(jres) - remsetsed
	        remsetsed = 0.
	      else
	        remsetsed = remsetsed - res_san(jres)
	        res_san(jres) = 0.
              if (res_sag(jres) >= remsetsed) then
	          res_sag(jres) = res_sag(jres) - remsetsed
	          remsetsed = 0.
	        else
	          remsetsed = remsetsed - res_sag(jres)
	          res_sag(jres) = 0.
                if (res_sil(jres) >= remsetsed) then
  	            res_sil(jres) = res_sil(jres) - remsetsed
	            remsetsed = 0.
	          else
	            remsetsed = remsetsed - res_sil(jres)
	            res_sil(jres) = 0.
                  if (res_cla(jres) >= remsetsed) then
	              res_cla(jres) = res_cla(jres) - remsetsed
	              remsetsed = 0.
	            else
	              remsetsed = remsetsed - res_cla(jres)
	              res_cla(jres) = 0.
	            end if
                end if
	        end if
	      end if
	    end if
	  endif

        end if

        !liu
        !! compute sediment leaving reservoir
        !ressedo = res_sed(jres) * resflwo
        !ressano = res_san(jres) * resflwo
        !ressilo = res_sil(jres) * resflwo
        !resclao = res_cla(jres) * resflwo
        !ressago = res_sag(jres) * resflwo
        !reslago = res_lag(jres) * resflwo
	  !resgrao = res_gra(jres) * resflwo
        !liu

        !! net change in amount of sediment in reservoir for day
        ressedc = vol * sed + ressedi - ressedo - res_sed(jres) *       &
     &                                                     res_vol(jres)
!      write (130,5999) i, jres, res_sed(jres), sed_stlr(jres),          &
!     & res_nsed(jres), ressedi, ressedo, resflwi, resflwo
!5999  format (2i4,7e12.4)
      
      end if

!!    update surface area for day
    !  ressa = br1(jres) * res_vol(jres) ** br2(jres)
      if (iresco(jres)<4) then       !liu  
!!    update surface area for day
      ressa = br1(jres) * res_vol(jres) ** br2(jres)
      end if                    !liu  
      return
 5000 format (f8.2)
      end
