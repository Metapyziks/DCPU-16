			; Hello world for DCPU-16
			; By Metapyziks

			set i, 0					; Initialise loop
nextchar:
			ife [data+i], 0				; If we have reached the end of the string
				set PC, end				; Goto the end
			
            set a, [data+i]				; Load the next character
			bor a, 0x0300				; Set the colour to white
            set [0x8000+i], a			; Set the value in video memory
            add i, 1					; Increment the loop
            set PC, nextchar			; Loop back to do the next character
data:       
			dat "Hello world!", 0		; Null terminated string to print
end:
			set PC, end					; Hang forever
