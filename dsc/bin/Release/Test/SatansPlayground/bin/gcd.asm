.globl rem

# Int32 rem(Int32 x, Int32 y)
rem:
    sw $fp, ($sp) # push old frame pointer (dynamic link)
    move $fp, $sp # frame pointer now points to the top of the stack
    subu $sp, $sp, 20 # allocate 20 bytes on the stack
    sw $ra, -4($fp) # store the value of the return address
    sw $v0, -8($fp) # store the static link

    sw $s0, -12($fp) # save locally used registers
    sw $s1, -16($fp)

    # moves arguments to locals
    # x --> $s0
    move $s0, $a0
    # y --> $s1
    move $s1, $a1

    rem $t0, $s0, $s1
    # place result in return value location
    move $v0, $t0

    lw $s1, -20($fp) # reset saved register $s1
    lw $s0, -16($fp) # reset saved register $s0
    lw $ra, -4($fp) # get return address from frame
    move $sp, $fp # get old frame pointer from current frame
    lw $fp, ($sp) # restore old frame pointer
    jr $ra # jumps back to caller