class Operations:

    def negate(self, Value):
        return -Value

    def minus_one(self):
        return -1

    def add(self, Left, Right):
        for a, b in zip(Left, Right):
            yield a + b