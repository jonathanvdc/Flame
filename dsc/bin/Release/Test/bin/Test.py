class IList:
    """ Describes a generic list. """

    def add(self, Item):
        """ Adds an item to the end of the list. """
        raise NotImplementedError("Method 'IList.add' was not implemented.")

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        raise NotImplementedError("Method 'IList.insert' was not implemented.")

    def get_length(self):
        """ Gets the length of the list. """
        raise NotImplementedError("Method 'IList.get_length' was not implemented.")


class List(IList):
    """ An array-based implementation of a list. """

    def __init__(self):
        """ Creates a new instance of a list. """
        self.data = [None] * 5
        self.count = 0

    def add(self, Item):
        """ Adds an item to the end of the list. """
        if not self.get_length() < len(self.data):
            newData = [None] * (len(self.data) + 5)
            self.copy_to(newData)
            self.data = newData
        self.data[self.count] = Item
        self.count += 1

    def copy_to(self, Target):
        list = self.data
        i = len(list)
        j = 0
        k = len(Target)
        while (j < i) & (j < k):
            Target[j] = list[j]
            j += 1

    def insert(self, Index, Item):
        if Index == self.get_length():
            self.add(Item)
            return True
        else:
            if Index < self.get_length():
                self.shift_right(Index)
                self.data[Index] = Item
                self.count += 1
                return True
            else:
                return False

    def shift_right(self, StartIndex):
        if not self.get_length() < len(self.data):
            newData = [None] * (len(self.data) + 5)
            i = 0
            while i < StartIndex:
                newData[i] = self.data[i]
                i += 1
            i = StartIndex
            while i < self.get_length():
                newData[i + 1] = self.data[i]
                i += 1
            self.data = newData
        else:
            i = StartIndex
            while i < self.get_length():
                self.data[i + 1] = self.data[i]
                i += 1

    def remove_at(self, Index):
        if Index > 0 and Index < self.get_length():
            self.shift_left(Index)
            self.count -= 1
            return True
        else:
            return False

    def shift_left(self, StartIndex):
        i = StartIndex + 1
        while i < self.get_length():
            self.data[i - 1] = self.data[i]
            i += 1

    def contains(self, Item):
        i = 0
        while i < self.get_length():
            if self.data[i] == Item:
                return True
            i += 1
        return False

    def to_array(self):
        arr = [None] * self.get_length()
        self.copy_to(arr)
        return arr

    def get_length(self):
        return self.count