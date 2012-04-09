			; Key input test
			; By Metapyziks

			set i, 0
:start
			ife [0x8200], 0x00
				set PC, start
			
			set a, [0x8200]
			set [0x8200], 0x00
			
			ife a, 0x8
				set PC, backspace
			
			ife a, 0xd
				set PC, return
			
			ife a, 0x7f
				set PC, start
			
			ifg 0x20, a
				set PC, start
			
			bor a, 0x0300
			set [0x8000+i], a
			add i, 1
			
			ife i, 0x200
				set i, 0
			
			set PC, start
:backspace
			ife i, 0
				set PC, start
			
			sub i, 1
			set [0x8000+i], 0x0000
			
			set PC, start
:return
			div i, 0x20
			mul i, 0x20
			add i, 0x20
			
			ife i, 0x200
				set i, 0
			
			set PC, start
