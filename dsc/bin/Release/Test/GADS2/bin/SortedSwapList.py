from ISortedList import *

class SortedSwapList(ISortedList):
    """ A sorted list whose backing list can be swapped.
        It is the sorted equivalent of 'SwapList<T>' """

    def __init__(self, backingList):
        """ Creates a new sorted swap list with the specified backing list. """
        self.backing_list = backingList

    def swap(self, Container):
        """ Swaps the sorted swap list's backing container with another backing list. """
        # Pre:
        # 'Container' should be empty.
        # If not, it will be cleared.
        # Post:
        # The container is cleared.
        # Then, all items in the current backing list are copied to 'Container'.
        # After that, the backing list of this sorted swap list is set to 'Container'.
        preItems = Container.to_list()
        i = 0
        while i < preItems.count:
            Container.remove(preItems[i])
            i += 1
        items = self.backing_list.to_list()
        self.binary_copy(items, Container, 0, items.count)
        self.backing_list = Container

    def binary_copy(self, Source, Target, StartIndex, Count):
        """ A recursive algorithm that copies all items from a read-only list to the given target list.
            This is intended to maximize performance when switching to a backing sorted list implemented by a binary tree.
            This method is private. """
        if Count > 0:
            mid = StartIndex + Count // 2
            Target.add(Source[mid])
            leftCount = mid
            self.binary_copy(Source, Target, StartIndex, leftCount)
            rightCount = StartIndex + Count - mid - 1
            self.binary_copy(Source, Target, mid + 1, rightCount)

    def add(self, Item):
        """ Adds an item to the collection. """
        self.backing_list.add(Item)

    def remove(self, Item):
        """ Removes an item from the list. """
        # Pre:
        # For Item to be successfully removed from the sorted list, it must have been in the list before removal.
        # Post:
        # Returns true if the sorted list contained the given item, false if not.
        # If this returns true, the item has been removed from the sorted list.
        return self.backing_list.remove(Item)

    def contains(self, Item):
        """ Finds out if the sorted list contains the given item. """
        return self.backing_list.contains(Item)

    def to_list(self):
        """ Returns a read-only list that represents this list's contents, for easy enumeration. """
        return self.backing_list.to_list()

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.backing_list.__iter__()

    @property
    def key_map(self):
        """ Gets a record-to-key map that maps records to sortable keys. """
        return self.backing_list.key_map

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates if the sorted list is empty. """
        # Post:
        # Return true if empty, false if not.
        return self.backing_list.is_empty

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.backing_list.count