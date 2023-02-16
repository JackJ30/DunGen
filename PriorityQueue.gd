extends RefCounted
class_name PriorityQueue

# Simple subclass to hold the queued queue data and priority
class QueueItem:
	var data
	var priority : float

	func _init(data, priority : float) -> void:
		self.data = data
		self.priority = priority

var queue : Array

func _init():
	self.queue = Array()

# add data to queue with a given priority
func enqueue(data, priority : float) -> void:
	var new_item = QueueItem.new(data, priority)
	insert(new_item)
	# we can assume new element has the highest priority, just append
	

# removes first item
func dequeue():
	if self.empty() == true:
		# print_debug('Cannot dequeue: queue is empty')
		return null

	return self.queue.pop_front().data

func update_priority(element, priority : float):
	var to_update = queue.filter(func(n): return n.data == element)[0]
	queue.erase(to_update)
	insert(to_update)

func insert(item : QueueItem):
	for i in range(queue.size()):
		# if the new item has a lower priority, it goes first
		if self.queue[i].priority > item.priority:
			self.queue.insert(i, item)
			return
	self.queue.append(item)

# remove last item
func dequeue_back():
	if self.empty() == true:
		# print_debug('Cannot dequeue back: queue is empty')
		return null
	return self.queue.pop_back().data

# remove all items
func clear():
	queue.clear()

# returns the front item, but doesn't remove it from the queue
func front():
	if self.empty() == true:
		# print_debug('No elements are in queue')
		return null

	return self.queue.front().data

# returns the last item, but doesn't remove it from the queue
func back():
	if self.empty() == true:
		# print_debug('No elements are in queue')
		return null

	return self.queue.back().data

# returns whether or not the queue is empty
func empty() -> bool:
	return self.queue.size() == 0

func contains(element) -> bool:
	return !queue.filter(func(n): return n.data == element).is_empty()

# returns the number of items in the queue
func size() -> int:
	return self.queue.size()
	
