			; Rainbow display test
			; By Metapyziks

			set x, 0
:xloop
			set y, 0
:yloop
			set c, x
			div c, 2
			set a, c
			shl a, 0x4
			add a, y
			shl a, 0x8
			set c, x
			and c, 0x1
			add a, [chars+c]
			
			set b, y
			mul b, [width]
			add b, x
			set [0x8000+b], a
			
			add y, 1
			ifn y, [height]
				set PC, yloop
				
			add x, 1
			ifn x, [width]
				set PC, xloop
:end
			set PC, end

:chars		dat 0x01, 0x02
:width		dat 0x20
:height		dat 0x10
