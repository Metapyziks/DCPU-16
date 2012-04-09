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
			add a, 'X'
			
			set b, y
			mul b, 0x20
			add b, x
			set [0x8000+b], a
			
			add y, 1
			ifn y, 0x10
				set PC, yloop
				
			add x, 1
			ifn x, 0x20
				set PC, xloop
:end				
			set PC, end
