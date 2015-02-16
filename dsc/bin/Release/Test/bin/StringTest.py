class StringTest:

    def __init__(self, val):
        self.val = 0
        self.val = val

    def set_to_a(self):
        self.set_char_val("\\")

    def to_string(self):
        return chr(self.val)

    def to_float32(self):
        return unpack('f', pack('l', self.val))[0]

    def ends_with(self, Value):
        return self.to_string().endswith(Value)

    def set_char_val(self, value):
        self.val = ord(value)

    def get_char_val(self):
        return chr(self.val)

    def get_value(self):
        return self.val

    def set_value(self, value):
        self.val = value

    def get_string_length(self):
        return len(self.get_char_val())