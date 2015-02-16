.globl ArrayOf
.globl AddArrays

ArrayOf:
    sw $fp, ($sp) # push old frame pointer (dynamic link)
    move $fp, $sp # frame pointer now points to the top of the stack
    subu $sp, $sp, 24 # allocate 24 bytes on the stack
    sw $ra, -4($fp) # store the value of the return address
    sw $v0, -8($fp) # store the static link

    sw $s0, -12($fp) # save locally used registers
    sw $s1, -16($fp)
    sw $s2, -20($fp)

    # moves arguments to locals
    # A --> $s0
    move $s0, $a0
    # B --> $s1
    move $s1, $a1

    # moves argument values into argument locations
    li $a0, 2

    li $t0, 4
    mul $t1, $a0, $t0
    move $a0, $t1
    li $v0, 9
    syscall # issues a system call

    move $s2, $v0
    sw $s0, ($s2)
    li $v0, 4
    li $a0, 1
    mul $t1, $a0, $v0
    add $v0, $s2, $t1
    sw $s1, ($v0)
    # place result in return value location
    move $v0, $s2

    lw $s2, -24($fp) # reset saved register $s2
    lw $s1, -20($fp) # reset saved register $s1
    lw $s0, -16($fp) # reset saved register $s0
    lw $ra, -4($fp) # get return address from frame
    move $sp, $fp # get old frame pointer from current frame
    lw $fp, ($sp) # restore old frame pointer
    jr $ra # jumps back to caller

AddArrays:
    sw $fp, ($sp) # push old frame pointer (dynamic link)
    move $fp, $sp # frame pointer now points to the top of the stack
    subu $sp, $sp, 32 # allocate 32 bytes on the stack
    sw $ra, -4($fp) # store the value of the return address
    sw $v0, -8($fp) # store the static link

    sw $s0, -12($fp) # save locally used registers
    sw $s1, -16($fp)
    sw $s2, -20($fp)
    sw $s3, -24($fp)
    sw $s4, -28($fp)

    # moves arguments to locals
    # A --> $s0
    move $s0, $a0
    # B --> $s1
    move $s1, $a1
    # Length --> $s2
    move $s2, $a2

    # moves argument values into argument locations
    move $a0, $s2

    li $t0, 4
    mul $t1, $a0, $t0
    move $a0, $t1
    li $v0, 9
    syscall # issues a system call

    move $s3, $v0
    move $s4, $zero
    j AddArrays_label_0 # jumps to AddArrays_label_0
AddArrays_label_1:
    li $v0, 4
    mul $a0, $s4, $v0
    add $v0, $s3, $a0
    li $a0, 4
    mul $t1, $s4, $a0
    add $a0, $s0, $t1
    lw $t1, ($a0)
    li $t0, 4
    mul $t2, $s4, $t0
    add $t0, $s1, $t2
    lw $t2, ($t0)
    add $t1, $t1, $t2
    sw $t1, ($v0)
    li $t1, 1
    add $t2, $s4, $t1
    move $s4, $t2
AddArrays_label_0:
    blt $s4, $s2, AddArrays_label_1
AddArrays_label_2:
    # place result in return value location
    move $v0, $s3

    lw $s4, -32($fp) # reset saved register $s4
    lw $s3, -28($fp) # reset saved register $s3
    lw $s2, -24($fp) # reset saved register $s2
    lw $s1, -20($fp) # reset saved register $s1
    lw $s0, -16($fp) # reset saved register $s0
    lw $ra, -4($fp) # get return address from frame
    move $sp, $fp # get old frame pointer from current frame
    lw $fp, ($sp) # restore old frame pointer
    jr $ra # jumps back to caller