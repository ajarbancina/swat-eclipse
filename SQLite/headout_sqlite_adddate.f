      subroutine headout_sqlite_adddate(col,yearcolindex)

!!     ~ ~ ~ PURPOSE ~ ~ ~
!!     add date columns (year, month and day) to given column structure

      use parm

      type(SQLITE_COLUMN), intent(inout) :: col
      integer, intent(in)                :: yearcolindex

      call sqlite3_column_props( col(yearcolindex),
     &                                                  "YR",SQLITE_INT)
      if(iprint < 2) then       !!monthly or daily
        call sqlite3_column_props(col(yearcolindex+1),
     &                                                  "MO",SQLITE_INT)
        if(iprint == 1) then    !!daily
            call sqlite3_column_props(col(yearcolindex+2),
     &                                                  "DA",SQLITE_INT)
        end if
      end if

      end
