from IList import *

class ArrayList(IList):
    """ An array-based implementation of a list. """

    def __init__(self, data = None):
        """ Creates a new instance of a list backed by the provided array. """
        if data is None:
            self.data = [None] * 5
            self.elem_count = 0
            return
        self.elem_count = 0
        self.data = data
        self.elem_count = len(data)

    def add(self, Item):
        """ Adds an item to the end of the list. """
        if self.count >= len(self.data):
            newData = [None] * (len(self.data) + 5)
            self.copy_to(newData)
            self.data = newData
        self.data[self.elem_count] = Item
        self.elem_count += 1

    def copy_to(self, Target):
        """ Copies the array list's contents to the provided target array. """
        # Post:
        # Copies all elements from the list, starting at index 0 up to and including the last index of this list or the target array, whichever comes first.
        for i in range(min(len(self.data), len(Target))):
            Target[i] = self.data[i]

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the provided index was an invalid index in the list, the list's state is not changed, and false is returned.
        # Otherwise, all items from the given index upward are shifted once to the right, and the provided item is inserted at the specified index in the list.
        if Index == self.count:
            self.add(Item)
            return True
        elif Index < self.count and Index >= 0:
            self.shift_right(Index)
            self.data[Index] = Item
            self.elem_count += 1
            return True
        else:
            return False

    def shift_right(self, StartIndex):
        """ Shifts the elements in the list to the right from the provided index onward.
            This method is private. """
        if self.count >= len(self.data):
            newData = [None] * (len(self.data) + 5)
            i = 0
            while i < StartIndex:
                newData[i] = self.data[i]
                i += 1
            i = StartIndex
            while i < self.count:
                newData[i + 1] = self.data[i]
                i += 1
            self.data = newData
        else:
            i = self.count - 1
            while i >= StartIndex:
                self.data[i + 1] = self.data[i]
                i -= 1

    def remove_at(self, Index):
        """ Removes the element at the specified index from the list. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the provided index was an invalid index in the list, the list's state is not changed, and false is returned.
        # Otherwise, the item at the specified index is removed and the index of all items whose index is greater than the provided index, will be decremented by one.
        if Index >= 0 and Index < self.count:
            self.shift_left(Index)
            self.elem_count -= 1
            return True
        else:
            return False

    def shift_left(self, StartIndex):
        """ Shifts the elements in the list to the left from the provided index onward.
            This method is private. """
        i = StartIndex + 1
        while i < self.count:
            self.data[i - 1] = self.data[i]
            i += 1

    def to_array(self):
        """ Gets an array with length the number of elements in this list, and the same contents as this list. """
        # Remarks:
        # The resulting array is not the backing array for this list, but rather an array whereto the elements of this list are copied.
        # This method was added to make copying an 'ArrayList<T>' easier.
        # Creating a new 'ArrayList<T>' from an array is also faster than creating an empty list and then adding every element from the original list, as the internal array will not need expanding.
        arr = [None] * self.count
        self.copy_to(arr)
        return arr

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        i = 0
        while i < self.count:
            yield self.data[i]
            i += 1

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.elem_count

    def __getitem__(self, Index):
        """ Gets the item in the list at the specified position. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the operation on said index is performed.
        # Otherwise, an exception is thrown.
        return self.data[Index]

    def __setitem__(self, Index, value):
        """ Sets the item in the list at the specified position. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the operation on said index is performed.
        # Otherwise, an exception is thrown.
        self.data[Index] = value